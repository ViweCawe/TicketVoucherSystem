-- Run in the EXISTING voucher database after taking your normal backup. Additive; no voucher data is deleted.
SET XACT_ABORT ON;
IF OBJECT_ID('dbo.PosRedemption', 'U') IS NULL
BEGIN
CREATE TABLE dbo.PosRedemption
(
    Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PosRedemption PRIMARY KEY,
    ClientId varchar(64) COLLATE Latin1_General_100_BIN2 NOT NULL,
    IdempotencyKey uniqueidentifier NOT NULL,
    TransactionId varchar(64) COLLATE Latin1_General_100_BIN2 NOT NULL,
    VoucherId bigint NOT NULL,
    VoucherCode varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
    DepartmentType varchar(30) NOT NULL,
    OutletId int NOT NULL,
    TillId varchar(64) COLLATE Latin1_General_100_BIN2 NOT NULL,
    OperatorId varchar(64) COLLATE Latin1_General_100_BIN2 NOT NULL,
    SaleTotal decimal(12,2) NOT NULL,
    EligibleTotal decimal(12,2) NOT NULL,
    DiscountAmount decimal(12,2) NOT NULL,
    AmountDue AS (SaleTotal - DiscountAmount) PERSISTED,
    Currency char(3) NOT NULL CONSTRAINT DF_PosRedemption_Currency DEFAULT ('ZAR'),
    RedeemedUtc datetime2(0) NOT NULL,
    CONSTRAINT FK_PosRedemption_Voucher FOREIGN KEY (VoucherId) REFERENCES dbo.Voucher(Id),
    CONSTRAINT FK_PosRedemption_Outlet FOREIGN KEY (OutletId) REFERENCES dbo.Outlet(Id),
    CONSTRAINT UQ_PosRedemption_Key UNIQUE (ClientId, IdempotencyKey),
    CONSTRAINT UQ_PosRedemption_Transaction UNIQUE (ClientId, TransactionId),
    CONSTRAINT UQ_PosRedemption_Voucher UNIQUE (VoucherId),
    CONSTRAINT CK_PosRedemption_Amounts CHECK (SaleTotal >= EligibleTotal AND EligibleTotal >= DiscountAmount AND DiscountAmount > 0),
    CONSTRAINT CK_PosRedemption_Currency CHECK (Currency = 'ZAR'),
    CONSTRAINT CK_PosRedemption_Department CHECK (DepartmentType IN ('Retail', 'FoodAndBeverage'))
);
END;
GO
CREATE OR ALTER PROCEDURE dbo.spPosVoucher_Lookup
    @Code varchar(32), @DepartmentType varchar(30), @OutletId int
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Outlet WHERE Id = @OutletId AND IsActive = 1)
        THROW 50006, 'This outlet is not active.', 1;
    -- Exact barcode and authenticated department, never a broad customer search.
    SELECT * FROM dbo.vVoucherDetails
    WHERE Code = @Code AND DepartmentType = @DepartmentType;
END;

GO
CREATE OR ALTER PROCEDURE dbo.spPosVoucher_Redeem
    @ClientId varchar(64), @TillId varchar(64), @OutletId int, @DepartmentType varchar(30),
    @IdempotencyKey uniqueidentifier, @TransactionId varchar(64), @OperatorId varchar(64),
    @VoucherCode varchar(32), @SaleTotal decimal(12,2), @EligibleTotal decimal(12,2), @Currency char(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @SaleTotal IS NULL OR @EligibleTotal IS NULL OR @SaleTotal <= 0 OR @EligibleTotal <= 0
       OR @EligibleTotal > @SaleTotal OR @Currency IS NULL OR @Currency <> 'ZAR'
       OR @IdempotencyKey IS NULL OR @IdempotencyKey = '00000000-0000-0000-0000-000000000000'
       OR NULLIF(LTRIM(RTRIM(@ClientId)), '') IS NULL OR NULLIF(LTRIM(RTRIM(@TillId)), '') IS NULL
       OR NULLIF(LTRIM(RTRIM(@TransactionId)), '') IS NULL OR NULLIF(LTRIM(RTRIM(@OperatorId)), '') IS NULL
       OR NULLIF(LTRIM(RTRIM(@VoucherCode)), '') IS NULL
       OR @DepartmentType IS NULL OR @DepartmentType NOT IN ('Retail', 'FoodAndBeverage')
        THROW 50104, 'Invalid redemption request.', 1;

    DECLARE @Id bigint, @VoucherId bigint, @Status varchar(20), @ExpiresUtc datetime2(0),
        @Amount decimal(12,2), @OutletName nvarchar(120), @Now datetime2(0), @LockResult int;
    DECLARE @LockName nvarchar(255) = CONCAT('POS-client:', @ClientId);
    DECLARE @PerformedBy nvarchar(256) = CONCAT('POS:', @ClientId, ':', @TillId, ':', @OperatorId);

    BEGIN TRY
        BEGIN TRANSACTION;
        -- Serialize requests from the same till; independent tills can run in parallel.
        -- This also prevents different keys being used simultaneously for the same sale.
        EXEC @LockResult = sys.sp_getapplock @Resource = @LockName, @LockMode = 'Exclusive',
            @LockOwner = 'Transaction', @LockTimeout = 10000;
        IF @LockResult < 0 THROW 50105, 'Redemption is busy. Retry the original request.', 1;

        SELECT @OutletName = Name FROM dbo.Outlet WITH (HOLDLOCK) WHERE Id = @OutletId AND IsActive = 1;
        IF @OutletName IS NULL THROW 50006, 'This outlet is not active.', 1;

        SELECT @Id = Id FROM dbo.PosRedemption WHERE ClientId = @ClientId AND IdempotencyKey = @IdempotencyKey;
        IF @Id IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.PosRedemption WHERE Id = @Id
                AND TransactionId = @TransactionId AND VoucherCode = @VoucherCode
                AND DepartmentType = @DepartmentType AND OutletId = @OutletId AND TillId = @TillId
                AND OperatorId = @OperatorId AND SaleTotal = @SaleTotal AND EligibleTotal = @EligibleTotal AND Currency = @Currency)
                THROW 50101, 'The idempotency key has different request details.', 1;
            COMMIT TRANSACTION;
            SELECT *, CAST(1 AS bit) AS IsReplay FROM dbo.PosRedemption WHERE Id = @Id;
            RETURN;
        END;
        IF EXISTS (SELECT 1 FROM dbo.PosRedemption WHERE ClientId = @ClientId AND TransactionId = @TransactionId)
            THROW 50102, 'This sale already has a voucher redemption.', 1;

        -- Same row lock as the existing tablet/web redemption: only one channel can win.
        SELECT @VoucherId = Id, @Status = Status, @ExpiresUtc = ExpiresUtc, @Amount = Amount
        FROM dbo.Voucher WITH (UPDLOCK, HOLDLOCK)
        WHERE Code = @VoucherCode AND DepartmentType = @DepartmentType;
        SET @Now = SYSUTCDATETIME();
        IF @VoucherId IS NULL THROW 50001, 'Voucher not found for this department.', 1;
        IF @Status = 'Redeemed' THROW 50002, 'This voucher has already been redeemed.', 1;
        IF @Status = 'Cancelled' THROW 50003, 'This voucher was cancelled.', 1;
        IF @Status = 'Expired' OR @ExpiresUtc <= @Now THROW 50004, 'This voucher has expired.', 1;
        IF @Status <> 'Issued' THROW 50005, 'This voucher cannot be redeemed.', 1;
        IF @EligibleTotal < @Amount THROW 50103, 'Eligible purchases must cover the full voucher amount.', 1;

        UPDATE dbo.Voucher SET Status = 'Redeemed', RedeemedUtc = @Now, RedeemedOutletId = @OutletId,
            RedeemedAt = @OutletName, RedeemedBy = @PerformedBy WHERE Id = @VoucherId AND Status = 'Issued';
        IF @@ROWCOUNT <> 1 THROW 50005, 'This voucher cannot be redeemed.', 1;
        INSERT dbo.PosRedemption (ClientId, IdempotencyKey, TransactionId, VoucherId, VoucherCode, DepartmentType,
            OutletId, TillId, OperatorId, SaleTotal, EligibleTotal, DiscountAmount, Currency, RedeemedUtc)
        VALUES (@ClientId, @IdempotencyKey, @TransactionId, @VoucherId, @VoucherCode, @DepartmentType,
            @OutletId, @TillId, @OperatorId, @SaleTotal, @EligibleTotal, @Amount, @Currency, @Now);
        SET @Id = SCOPE_IDENTITY();
        INSERT dbo.VoucherEvent (VoucherId, EventType, EventUtc, PerformedBy, Location, Notes)
        VALUES (@VoucherId, 'Redeemed', @Now, @PerformedBy, @OutletName, CONCAT('POS sale ', @TransactionId, '; receipt ', @Id));
        COMMIT TRANSACTION;
        SELECT *, CAST(0 AS bit) AS IsReplay FROM dbo.PosRedemption WHERE Id = @Id;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

GO

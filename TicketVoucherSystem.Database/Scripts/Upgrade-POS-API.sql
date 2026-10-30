SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
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
    AmountDue AS (SaleTotal - DiscountAmount),
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
    @Code varchar(32), @DepartmentType varchar(30)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vVoucherDetails WHERE Code = @Code AND DepartmentType = @DepartmentType;
END;

GO
CREATE OR ALTER PROCEDURE dbo.spPosVoucher_Redeem
    @Code varchar(32), @DepartmentType varchar(30)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Id bigint, @Status varchar(20), @Expiry datetime2(0), @Now datetime2(0);
    BEGIN TRY
        BEGIN TRANSACTION;
        SELECT @Id = Id, @Status = Status, @Expiry = ExpiresUtc
        FROM dbo.Voucher WITH (UPDLOCK, HOLDLOCK)
        WHERE Code = @Code AND DepartmentType = @DepartmentType;
        SET @Now = SYSUTCDATETIME();
        IF @Id IS NULL THROW 50001, 'Voucher not found.', 1;
        IF @Status = 'Redeemed' THROW 50002, 'Already redeemed.', 1;
        IF @Status = 'Cancelled' THROW 50003, 'Cancelled voucher.', 1;
        IF @Status = 'Expired' OR @Expiry <= @Now THROW 50004, 'Expired voucher.', 1;
        IF @Status <> 'Issued' THROW 50005, 'Voucher cannot be redeemed.', 1;
        UPDATE dbo.Voucher SET Status = 'Redeemed', RedeemedUtc = @Now WHERE Id = @Id;
        -- Existing audit schema requires PerformedBy; this is a system label, not a person or till.
        INSERT dbo.VoucherEvent(VoucherId, EventType, EventUtc, PerformedBy, Notes)
        VALUES (@Id, 'Redeemed', @Now, 'Public POS API', 'Redeemed through the public voucher endpoint.');
        COMMIT TRANSACTION;
        SELECT * FROM dbo.vVoucherDetails WHERE Id = @Id;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

GO

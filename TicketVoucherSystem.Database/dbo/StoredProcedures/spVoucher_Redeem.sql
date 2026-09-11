CREATE PROCEDURE dbo.spVoucher_Redeem
    @Code varchar(32),
    @DepartmentType varchar(30),
    @OutletId int,
    @UserName nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @VoucherId bigint, @Status varchar(20), @ExpiresUtc datetime2(0), @OutletName nvarchar(120);
    SELECT @OutletName = Name FROM dbo.Outlet WHERE Id = @OutletId AND IsActive = 1;
    IF @OutletName IS NULL
        THROW 50006, 'Select an active redemption outlet.', 1;

    BEGIN TRANSACTION;

    SELECT @VoucherId = Id, @Status = Status, @ExpiresUtc = ExpiresUtc
    FROM dbo.Voucher WITH (UPDLOCK, HOLDLOCK)
    WHERE Code = @Code AND DepartmentType = @DepartmentType;

    IF @VoucherId IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50001, 'Voucher not found for this department.', 1;
    END;

    IF @Status = 'Redeemed'
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50002, 'This voucher has already been redeemed.', 1;
    END;

    IF @Status = 'Cancelled'
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50003, 'This voucher was cancelled.', 1;
    END;

    IF @Status = 'Expired' OR @ExpiresUtc <= SYSUTCDATETIME()
    BEGIN
        IF @Status <> 'Expired'
        BEGIN
            UPDATE dbo.Voucher SET Status = 'Expired' WHERE Id = @VoucherId;
            INSERT dbo.VoucherEvent (VoucherId, EventType, PerformedBy, Location, Notes)
            VALUES (@VoucherId, 'Expired', @UserName, @OutletName, 'Redemption rejected because the voucher had expired.');
        END;

        COMMIT TRANSACTION;
        THROW 50004, 'This voucher has expired.', 1;
    END;

    UPDATE dbo.Voucher
    SET Status = 'Redeemed', RedeemedUtc = SYSUTCDATETIME(), RedeemedOutletId = @OutletId,
        RedeemedAt = @OutletName, RedeemedBy = @UserName
    WHERE Id = @VoucherId AND Status = 'Issued';

    IF @@ROWCOUNT <> 1
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50005, 'The voucher could not be redeemed.', 1;
    END;

    INSERT dbo.VoucherEvent (VoucherId, EventType, PerformedBy, Location)
    VALUES (@VoucherId, 'Redeemed', @UserName, @OutletName);

    COMMIT TRANSACTION;
    SELECT * FROM dbo.vVoucherDetails WHERE Id = @VoucherId;
END;

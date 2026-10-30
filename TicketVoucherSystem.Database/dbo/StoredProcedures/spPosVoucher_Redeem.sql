CREATE PROCEDURE dbo.spPosVoucher_Redeem
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

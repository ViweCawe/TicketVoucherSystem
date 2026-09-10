CREATE PROCEDURE dbo.spVoucher_Cancel
    @Id bigint,
    @Reason nvarchar(500),
    @UserName nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE dbo.Voucher WITH (UPDLOCK)
    SET Status = 'Cancelled',
        CancelledUtc = SYSUTCDATETIME(),
        CancelledBy = @UserName
    WHERE Id = @Id
      AND Status = 'Issued'
      AND ExpiresUtc > SYSUTCDATETIME();

    IF @@ROWCOUNT <> 1
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50040, 'Only an active, issued voucher can be cancelled.', 1;
    END;

    INSERT dbo.VoucherEvent (VoucherId, EventType, PerformedBy, Notes)
    VALUES (@Id, 'Cancelled', @UserName, @Reason);

    COMMIT TRANSACTION;
END;

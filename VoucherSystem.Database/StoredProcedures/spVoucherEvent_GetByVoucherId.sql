CREATE PROCEDURE dbo.spVoucherEvent_GetByVoucherId
    @VoucherId bigint
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, EventType, EventUtc, PerformedBy, Location, Notes
    FROM dbo.VoucherEvent
    WHERE VoucherId = @VoucherId
    ORDER BY EventUtc DESC, Id DESC;
END;

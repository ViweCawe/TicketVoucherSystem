CREATE PROCEDURE dbo.spVoucher_GetDashboard
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(CASE WHEN Status = 'Issued' THEN 1 END) AS IssuedCount,
        COUNT(CASE WHEN Status = 'Redeemed' THEN 1 END) AS RedeemedCount,
        COUNT(CASE WHEN Status = 'Expired' THEN 1 END) AS ExpiredCount,
        COUNT(CASE WHEN Status = 'Cancelled' THEN 1 END) AS CancelledCount,
        COALESCE(SUM(CASE WHEN Status = 'Issued' THEN Amount ELSE 0 END), 0) AS OutstandingValue,
        COALESCE(SUM(CASE WHEN Status = 'Redeemed' AND CAST(RedeemedUtc AS date) = CAST(SYSUTCDATETIME() AS date) THEN Amount ELSE 0 END), 0) AS RedeemedValueToday
    FROM dbo.vVoucherDetails;
END;

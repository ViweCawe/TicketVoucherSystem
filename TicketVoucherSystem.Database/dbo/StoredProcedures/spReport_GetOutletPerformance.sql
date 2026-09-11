CREATE PROCEDURE dbo.spReport_GetOutletPerformance
    @Days int = 30
AS
BEGIN
    SET NOCOUNT ON;
    SET @Days = CASE WHEN @Days IN (7, 30, 90) THEN @Days ELSE 30 END;

    DECLARE @FromUtc datetime2(0) = DATEADD(day, 1 - @Days, CONVERT(date, SYSUTCDATETIME()));
    DECLARE @Total decimal(12,2) =
    (
        SELECT COALESCE(SUM(Amount), 0)
        FROM dbo.Voucher
        WHERE Status = 'Redeemed' AND RedeemedUtc >= @FromUtc
    );

    SELECT
        v.RedeemedOutletId AS OutletId,
        COALESCE(outlet.Name, NULLIF(v.RedeemedAt, ''), 'Unknown outlet') AS OutletName,
        COUNT(*) AS RedeemedCount,
        SUM(v.Amount) AS RedeemedValue,
        CAST(CASE WHEN @Total = 0 THEN 0 ELSE 100.0 * SUM(v.Amount) / @Total END AS decimal(5,1)) AS SharePercent
    FROM dbo.Voucher v
    LEFT JOIN dbo.Outlet outlet ON outlet.Id = v.RedeemedOutletId
    WHERE v.Status = 'Redeemed' AND v.RedeemedUtc >= @FromUtc
    GROUP BY v.RedeemedOutletId, COALESCE(outlet.Name, NULLIF(v.RedeemedAt, ''), 'Unknown outlet')
    ORDER BY RedeemedValue DESC, OutletName;
END;

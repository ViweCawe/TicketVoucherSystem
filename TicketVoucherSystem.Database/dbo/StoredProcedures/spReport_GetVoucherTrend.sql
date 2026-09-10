CREATE PROCEDURE dbo.spReport_GetVoucherTrend
    @Days int = 30
AS
BEGIN
    SET NOCOUNT ON;
    SET @Days = CASE WHEN @Days IN (7, 30, 90) THEN @Days ELSE 30 END;

    DECLARE @FromDate date = DATEADD(day, 1 - @Days, CONVERT(date, SYSUTCDATETIME()));

    ;WITH Dates AS
    (
        SELECT @FromDate AS ReportDate
        UNION ALL
        SELECT DATEADD(day, 1, ReportDate)
        FROM Dates
        WHERE ReportDate < CONVERT(date, SYSUTCDATETIME())
    )
    SELECT
        dates.ReportDate,
        COALESCE(issued.IssuedCount, 0) AS IssuedCount,
        COALESCE(redeemed.RedeemedCount, 0) AS RedeemedCount,
        COALESCE(issued.IssuedValue, 0) AS IssuedValue,
        COALESCE(redeemed.RedeemedValue, 0) AS RedeemedValue
    FROM Dates dates
    OUTER APPLY
    (
        SELECT COUNT(*) AS IssuedCount, COALESCE(SUM(Amount), 0) AS IssuedValue
        FROM dbo.vVoucherDetails
        WHERE IssuedUtc >= dates.ReportDate
          AND IssuedUtc < DATEADD(day, 1, dates.ReportDate)
    ) issued
    OUTER APPLY
    (
        SELECT COUNT(*) AS RedeemedCount, COALESCE(SUM(Amount), 0) AS RedeemedValue
        FROM dbo.vVoucherDetails
        WHERE RedeemedUtc >= dates.ReportDate
          AND RedeemedUtc < DATEADD(day, 1, dates.ReportDate)
    ) redeemed
    ORDER BY dates.ReportDate
    OPTION (MAXRECURSION 100);
END;

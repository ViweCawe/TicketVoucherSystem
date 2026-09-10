CREATE PROCEDURE dbo.spReport_GetExecutiveSummary
    @Days int = 30
AS
BEGIN
    SET NOCOUNT ON;
    SET @Days = CASE WHEN @Days IN (7, 30, 90) THEN @Days ELSE 30 END;

    DECLARE @FromUtc datetime2(0) = DATEADD(day, 1 - @Days, CONVERT(date, SYSUTCDATETIME()));

    SELECT
        COUNT(*) AS TotalIssued,
        COUNT(CASE WHEN Status = 'Redeemed' THEN 1 END) AS TotalRedeemed,
        COUNT(CASE WHEN Status = 'Issued' THEN 1 END) AS TotalOutstanding,
        COALESCE(SUM(Amount), 0) AS IssuedValue,
        COALESCE(SUM(CASE WHEN Status = 'Redeemed' THEN Amount ELSE 0 END), 0) AS RedeemedValue,
        COALESCE(SUM(CASE WHEN Status = 'Issued' THEN Amount ELSE 0 END), 0) AS OutstandingValue,
        CAST(CASE WHEN COUNT(*) = 0 THEN 0 ELSE 100.0 * COUNT(CASE WHEN Status = 'Redeemed' THEN 1 END) / COUNT(*) END AS decimal(5, 1)) AS RedemptionRate,
        COUNT(CASE WHEN CONVERT(date, IssuedUtc) = CONVERT(date, SYSUTCDATETIME()) THEN 1 END) AS IssuedToday,
        COUNT(CASE WHEN CONVERT(date, RedeemedUtc) = CONVERT(date, SYSUTCDATETIME()) THEN 1 END) AS RedeemedToday
    FROM dbo.vVoucherDetails
    WHERE IssuedUtc >= @FromUtc;
END;

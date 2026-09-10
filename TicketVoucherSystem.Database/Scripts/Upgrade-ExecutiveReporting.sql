CREATE OR ALTER PROCEDURE dbo.spReport_GetExecutiveSummary
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
GO

CREATE OR ALTER PROCEDURE dbo.spReport_GetVoucherTrend
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
        WHERE IssuedUtc >= dates.ReportDate AND IssuedUtc < DATEADD(day, 1, dates.ReportDate)
    ) issued
    OUTER APPLY
    (
        SELECT COUNT(*) AS RedeemedCount, COALESCE(SUM(Amount), 0) AS RedeemedValue
        FROM dbo.vVoucherDetails
        WHERE RedeemedUtc >= dates.ReportDate AND RedeemedUtc < DATEADD(day, 1, dates.ReportDate)
    ) redeemed
    ORDER BY dates.ReportDate
    OPTION (MAXRECURSION 100);
END;
GO

CREATE OR ALTER PROCEDURE dbo.spReport_GetDepartmentPerformance
    @Days int = 30
AS
BEGIN
    SET NOCOUNT ON;
    SET @Days = CASE WHEN @Days IN (7, 30, 90) THEN @Days ELSE 30 END;
    DECLARE @FromUtc datetime2(0) = DATEADD(day, 1 - @Days, CONVERT(date, SYSUTCDATETIME()));

    SELECT
        DepartmentType,
        COUNT(*) AS IssuedCount,
        COUNT(CASE WHEN Status = 'Redeemed' THEN 1 END) AS RedeemedCount,
        COUNT(CASE WHEN Status = 'Issued' THEN 1 END) AS OutstandingCount,
        COALESCE(SUM(Amount), 0) AS IssuedValue,
        COALESCE(SUM(CASE WHEN Status = 'Redeemed' THEN Amount ELSE 0 END), 0) AS RedeemedValue
    FROM dbo.vVoucherDetails
    WHERE IssuedUtc >= @FromUtc
    GROUP BY DepartmentType
    ORDER BY RedeemedValue DESC, DepartmentType;
END;
GO

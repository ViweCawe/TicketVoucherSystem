CREATE PROCEDURE dbo.spReport_GetDepartmentPerformance
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

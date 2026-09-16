CREATE PROCEDURE dbo.spTicketPackage_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ticketPackage.Id,
        ticketPackage.Name,
        ticketPackage.Description,
        MAX(CASE WHEN voucherPackage.DepartmentType = 'Retail' THEN voucherPackage.Amount END) AS RetailAmount,
        MAX(CASE WHEN voucherPackage.DepartmentType = 'FoodAndBeverage' THEN voucherPackage.Amount END) AS FoodAndBeverageAmount,
        COALESCE(MAX(voucherPackage.ValidDays), 365) AS ValidDays,
        ticketPackage.IsActive,
        CONCAT_WS(' + ',
            MAX(CASE WHEN voucherPackage.DepartmentType = 'Retail' THEN CONCAT('Retail R', CONVERT(varchar(32), voucherPackage.Amount)) END),
            MAX(CASE WHEN voucherPackage.DepartmentType = 'FoodAndBeverage' THEN CONCAT('F&B R', CONVERT(varchar(32), voucherPackage.Amount)) END)) AS BenefitSummary,
        COUNT(voucherPackage.Id) AS BenefitCount
    FROM dbo.TicketPackage ticketPackage
    LEFT JOIN dbo.TicketPackageBenefit benefit ON benefit.TicketPackageId = ticketPackage.Id
    LEFT JOIN dbo.VoucherPackage voucherPackage ON voucherPackage.Id = benefit.VoucherPackageId
    GROUP BY ticketPackage.Id, ticketPackage.Name, ticketPackage.Description, ticketPackage.IsActive
    ORDER BY ticketPackage.Name;
END;

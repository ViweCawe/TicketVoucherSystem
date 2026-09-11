CREATE PROCEDURE dbo.spTicketPackage_GetActive
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ticketPackage.Id,
        ticketPackage.Name,
        ticketPackage.Description,
        STRING_AGG(CONCAT(voucherPackage.Name, ' - R', CONVERT(varchar(20), voucherPackage.Amount)), ', ')
            WITHIN GROUP (ORDER BY benefit.DisplayOrder, voucherPackage.Name) AS BenefitSummary,
        COUNT(*) AS BenefitCount
    FROM dbo.TicketPackage ticketPackage
    INNER JOIN dbo.TicketPackageBenefit benefit ON benefit.TicketPackageId = ticketPackage.Id
    INNER JOIN dbo.VoucherPackage voucherPackage ON voucherPackage.Id = benefit.VoucherPackageId
    WHERE ticketPackage.IsActive = 1 AND voucherPackage.IsActive = 1
    GROUP BY ticketPackage.Id, ticketPackage.Name, ticketPackage.Description
    ORDER BY ticketPackage.Name;
END;

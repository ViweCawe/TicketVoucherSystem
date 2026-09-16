CREATE PROCEDURE dbo.spTicketVoucherIssue_GetBatch
    @BatchId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SELECT issue.Id, issue.BatchId, issue.TicketPackageId, ticketPackage.Name AS PackageName,
           ticketPackage.Description AS PackageDescription, issue.TicketNumber, issue.IssuedUtc, issue.IssuedBy
    FROM dbo.TicketVoucherIssue issue
    INNER JOIN dbo.TicketPackage ticketPackage ON ticketPackage.Id = issue.TicketPackageId
    WHERE issue.BatchId = @BatchId
    ORDER BY issue.Id;
END;

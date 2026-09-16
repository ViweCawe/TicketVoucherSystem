CREATE PROCEDURE dbo.spTicketVoucherIssue_GetBatchVouchers
    @BatchId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SELECT details.*
    FROM dbo.vVoucherDetails details
    INNER JOIN dbo.TicketVoucherIssue issue ON issue.Id = details.TicketIssueId
    WHERE issue.BatchId = @BatchId
    ORDER BY issue.Id, details.DepartmentType;
END;

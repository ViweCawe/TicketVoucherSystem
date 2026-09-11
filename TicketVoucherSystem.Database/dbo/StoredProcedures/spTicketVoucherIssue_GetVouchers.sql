CREATE PROCEDURE dbo.spTicketVoucherIssue_GetVouchers
    @Id bigint
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vVoucherDetails WHERE TicketIssueId = @Id ORDER BY DepartmentType;
END;

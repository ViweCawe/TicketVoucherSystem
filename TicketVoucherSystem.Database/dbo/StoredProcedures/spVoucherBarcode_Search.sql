CREATE PROCEDURE dbo.spVoucherBarcode_Search
    @Search nvarchar(100) = NULL,
    @Status varchar(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (500) Id, Code, DepartmentType, Status, ImportedUtc, ImportedBy, AssignedUtc, AssignedBy
    FROM dbo.VoucherBarcode
    WHERE (@Status IS NULL OR @Status = '' OR Status = @Status)
      AND (@Search IS NULL OR @Search = '' OR Code LIKE '%' + @Search + '%')
    ORDER BY ImportedUtc DESC, Id DESC;
END;

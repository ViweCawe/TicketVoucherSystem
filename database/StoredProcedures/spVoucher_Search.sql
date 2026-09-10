CREATE PROCEDURE dbo.spVoucher_Search
    @Search nvarchar(100) = NULL,
    @Status varchar(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (500) *
    FROM dbo.vVoucherDetails
    WHERE (@Status IS NULL OR @Status = '' OR Status = @Status)
      AND
      (
          @Search IS NULL OR @Search = ''
          OR Code LIKE '%' + @Search + '%'
          OR PackageName LIKE '%' + @Search + '%'
      )
    ORDER BY IssuedUtc DESC, Id DESC;
END;

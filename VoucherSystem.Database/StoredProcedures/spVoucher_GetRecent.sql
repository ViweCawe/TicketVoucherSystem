CREATE PROCEDURE dbo.spVoucher_GetRecent
    @Count int = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (CASE WHEN @Count BETWEEN 1 AND 100 THEN @Count ELSE 10 END) *
    FROM dbo.vVoucherDetails
    ORDER BY IssuedUtc DESC, Id DESC;
END;

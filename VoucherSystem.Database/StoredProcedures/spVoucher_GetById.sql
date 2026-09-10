CREATE PROCEDURE dbo.spVoucher_GetById
    @Id bigint
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM dbo.vVoucherDetails WHERE Id = @Id;
END;

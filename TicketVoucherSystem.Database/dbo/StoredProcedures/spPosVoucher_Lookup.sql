CREATE PROCEDURE dbo.spPosVoucher_Lookup
    @Code varchar(32), @DepartmentType varchar(30)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vVoucherDetails WHERE Code = @Code AND DepartmentType = @DepartmentType;
END;

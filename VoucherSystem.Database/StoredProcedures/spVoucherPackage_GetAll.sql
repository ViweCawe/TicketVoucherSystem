CREATE PROCEDURE dbo.spVoucherPackage_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Description, DepartmentType, Amount, ValidDays, IsActive
    FROM dbo.VoucherPackage
    ORDER BY DepartmentType, Amount, Name;
END;

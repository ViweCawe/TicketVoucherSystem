CREATE PROCEDURE dbo.spVoucherPackage_GetActive
    @DepartmentType varchar(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Description, DepartmentType, Amount, ValidDays, IsActive
    FROM dbo.VoucherPackage
    WHERE IsActive = 1
      AND (@DepartmentType IS NULL OR DepartmentType = @DepartmentType)
    ORDER BY DepartmentType, Amount, Name;
END;

CREATE PROCEDURE dbo.spVoucherPackage_Save
    @Id int,
    @Name nvarchar(80),
    @Description nvarchar(200),
    @DepartmentType varchar(30),
    @Amount decimal(12,2),
    @ValidDays int,
    @UserName nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON;

    IF @DepartmentType NOT IN ('Retail', 'FoodAndBeverage')
        THROW 50010, 'Invalid voucher department.', 1;

    IF @Amount <= 0 OR @ValidDays NOT BETWEEN 1 AND 3650
        THROW 50011, 'Invalid voucher value or validity period.', 1;

    IF @Id = 0
    BEGIN
        INSERT dbo.VoucherPackage
            (Name, Description, DepartmentType, Amount, ValidDays, CreatedBy)
        VALUES
            (@Name, @Description, @DepartmentType, @Amount, @ValidDays, @UserName);
        RETURN;
    END;

    UPDATE dbo.VoucherPackage
    SET Name = @Name,
        Description = @Description,
        DepartmentType = @DepartmentType,
        Amount = @Amount,
        ValidDays = @ValidDays,
        UpdatedUtc = SYSUTCDATETIME(),
        UpdatedBy = @UserName
    WHERE Id = @Id;
END;

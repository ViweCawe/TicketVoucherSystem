CREATE PROCEDURE dbo.spVoucherPackage_SetActive
    @Id int,
    @IsActive bit,
    @UserName nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.VoucherPackage
    SET IsActive = @IsActive,
        UpdatedUtc = SYSUTCDATETIME(),
        UpdatedBy = @UserName
    WHERE Id = @Id;
END;

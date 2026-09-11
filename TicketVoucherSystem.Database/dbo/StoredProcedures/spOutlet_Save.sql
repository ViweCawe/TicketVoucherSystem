CREATE PROCEDURE dbo.spOutlet_Save
    @Id int = NULL,
    @Name nvarchar(120),
    @Code varchar(20),
    @DisplayOrder int
AS
BEGIN
    SET NOCOUNT ON;
    IF NULLIF(LTRIM(RTRIM(@Name)), '') IS NULL OR NULLIF(LTRIM(RTRIM(@Code)), '') IS NULL
        THROW 50060, 'Outlet name and code are required.', 1;

    IF EXISTS
    (
        SELECT 1 FROM dbo.Outlet
        WHERE Id <> COALESCE(@Id, -1)
          AND (Name = LTRIM(RTRIM(@Name)) OR Code = UPPER(LTRIM(RTRIM(@Code))))
    )
        THROW 50062, 'Another outlet already uses this name or code.', 1;

    IF @Id IS NULL
        INSERT dbo.Outlet (Name, Code, DisplayOrder) VALUES (LTRIM(RTRIM(@Name)), UPPER(LTRIM(RTRIM(@Code))), @DisplayOrder);
    ELSE IF EXISTS (SELECT 1 FROM dbo.Outlet WHERE Id = @Id)
        UPDATE dbo.Outlet SET Name = LTRIM(RTRIM(@Name)), Code = UPPER(LTRIM(RTRIM(@Code))), DisplayOrder = @DisplayOrder WHERE Id = @Id;
    ELSE
        THROW 50061, 'Outlet not found.', 1;
END;

CREATE PROCEDURE dbo.spOutlet_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Code, IsActive, DisplayOrder FROM dbo.Outlet ORDER BY DisplayOrder, Name;
END;

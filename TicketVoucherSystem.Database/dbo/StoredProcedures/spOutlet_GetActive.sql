CREATE PROCEDURE dbo.spOutlet_GetActive
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Code, IsActive, DisplayOrder FROM dbo.Outlet
    WHERE IsActive = 1 ORDER BY DisplayOrder, Name;
END;

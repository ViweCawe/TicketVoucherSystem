CREATE PROCEDURE dbo.spOutlet_SetActive
    @Id int,
    @IsActive bit
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Outlet SET IsActive = @IsActive WHERE Id = @Id;
    IF @@ROWCOUNT = 0 THROW 50061, 'Outlet not found.', 1;
END;

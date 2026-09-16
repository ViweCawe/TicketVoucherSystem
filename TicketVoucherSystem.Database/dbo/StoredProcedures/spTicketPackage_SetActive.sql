CREATE PROCEDURE dbo.spTicketPackage_SetActive
    @Id int,
    @IsActive bit,
    @UserName nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.TicketPackage SET IsActive = @IsActive WHERE Id = @Id;
    IF @@ROWCOUNT = 0 THROW 50044, 'Ticket package not found.', 1;
END;

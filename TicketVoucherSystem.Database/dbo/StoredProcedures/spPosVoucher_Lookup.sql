CREATE PROCEDURE dbo.spPosVoucher_Lookup
    @Code varchar(32), @DepartmentType varchar(30), @OutletId int
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Outlet WHERE Id = @OutletId AND IsActive = 1)
        THROW 50006, 'This outlet is not active.', 1;
    -- Exact barcode and authenticated department, never a broad customer search.
    SELECT * FROM dbo.vVoucherDetails
    WHERE Code = @Code AND DepartmentType = @DepartmentType;
END;

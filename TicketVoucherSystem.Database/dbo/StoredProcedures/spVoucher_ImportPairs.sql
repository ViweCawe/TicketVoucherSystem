CREATE PROCEDURE dbo.spVoucher_ImportPairs
    @CodesJson nvarchar(max),
    @RetailPackageId int,
    @FoodPackageId int,
    @UserName nvarchar(256)
AS
BEGIN
    THROW 50034, 'Direct voucher creation from import is disabled. Import barcode stock, then issue vouchers from the allocation page.', 1;
END;

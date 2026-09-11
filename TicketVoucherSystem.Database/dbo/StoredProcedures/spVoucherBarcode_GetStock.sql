CREATE PROCEDURE dbo.spVoucherBarcode_GetStock
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        COUNT(CASE WHEN DepartmentType = 'Retail' AND Status = 'Available' THEN 1 END) AS AvailableRetail,
        COUNT(CASE WHEN DepartmentType = 'FoodAndBeverage' AND Status = 'Available' THEN 1 END) AS AvailableFoodAndBeverage,
        COUNT(CASE WHEN DepartmentType = 'Retail' AND Status = 'Assigned' THEN 1 END) AS AssignedRetail,
        COUNT(CASE WHEN DepartmentType = 'FoodAndBeverage' AND Status = 'Assigned' THEN 1 END) AS AssignedFoodAndBeverage
    FROM dbo.VoucherBarcode;
END;

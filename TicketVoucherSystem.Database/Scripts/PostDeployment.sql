IF NOT EXISTS (SELECT 1 FROM dbo.VoucherPackage)
BEGIN
    INSERT dbo.VoucherPackage
        (Name, Description, DepartmentType, Amount, ValidDays, CreatedBy)
    VALUES
        (N'Everyday R100', N'Everyday essentials voucher', 'Retail', 100.00, 365, N'Seed'),
        (N'Retail R250', N'Retail mid-value voucher', 'Retail', 250.00, 365, N'Seed'),
        (N'Choice R250', N'A flexible mid-value reward', 'Retail', 250.00, 365, N'Seed'),
        (N'Premium R500', N'Premium retail recognition package', 'Retail', 500.00, 365, N'Seed'),
        (N'F&B R100', N'Food and beverage voucher', 'FoodAndBeverage', 100.00, 365, N'Seed'),
        (N'F&B R250', N'Dining and refreshment value', 'FoodAndBeverage', 250.00, 365, N'Seed');
END;

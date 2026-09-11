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

IF NOT EXISTS (SELECT 1 FROM dbo.VoucherPackage WHERE Name = N'F&B R100')
    INSERT dbo.VoucherPackage (Name, Description, DepartmentType, Amount, ValidDays, CreatedBy)
    VALUES (N'F&B R100', N'R100 off food and beverage purchases', 'FoodAndBeverage', 100.00, 365, N'Seed');

IF NOT EXISTS (SELECT 1 FROM dbo.VoucherPackage WHERE Name = N'Retail R250')
    INSERT dbo.VoucherPackage (Name, Description, DepartmentType, Amount, ValidDays, CreatedBy)
    VALUES (N'Retail R250', N'R250 off retail purchases', 'Retail', 250.00, 365, N'Seed');

IF NOT EXISTS (SELECT 1 FROM dbo.TicketPackage WHERE Name = N'VIP Ticket')
    INSERT dbo.TicketPackage (Name, Description, CreatedBy)
    VALUES (N'VIP Ticket', N'Fast-track ticket with F&B and retail voucher benefits', N'Seed');

DECLARE @VipTicketPackageId int = (SELECT Id FROM dbo.TicketPackage WHERE Name = N'VIP Ticket');
DECLARE @FnbBenefitId int = (SELECT Id FROM dbo.VoucherPackage WHERE Name = N'F&B R100');
DECLARE @RetailBenefitId int = (SELECT Id FROM dbo.VoucherPackage WHERE Name = N'Retail R250');

IF NOT EXISTS (SELECT 1 FROM dbo.TicketPackageBenefit WHERE TicketPackageId = @VipTicketPackageId AND VoucherPackageId = @FnbBenefitId)
    INSERT dbo.TicketPackageBenefit (TicketPackageId, VoucherPackageId, DisplayOrder) VALUES (@VipTicketPackageId, @FnbBenefitId, 10);

IF NOT EXISTS (SELECT 1 FROM dbo.TicketPackageBenefit WHERE TicketPackageId = @VipTicketPackageId AND VoucherPackageId = @RetailBenefitId)
    INSERT dbo.TicketPackageBenefit (TicketPackageId, VoucherPackageId, DisplayOrder) VALUES (@VipTicketPackageId, @RetailBenefitId, 20);

IF NOT EXISTS (SELECT 1 FROM dbo.Outlet)
BEGIN
    INSERT dbo.Outlet (Name, Code, DisplayOrder)
    VALUES
        (N'Vista', 'VISTA', 10),
        (N'Upper Cableway', 'UPPER', 20),
        (N'Lower Cableway', 'LOWER', 30),
        (N'Table Mountain Cafe', 'CAFE', 40),
        (N'Retail Shop', 'RETAIL', 50);
END;

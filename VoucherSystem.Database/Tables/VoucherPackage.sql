CREATE TABLE dbo.VoucherPackage
(
    Id              int IDENTITY(1,1) NOT NULL CONSTRAINT PK_VoucherPackage PRIMARY KEY,
    Name            nvarchar(80) NOT NULL,
    Description     nvarchar(200) NOT NULL CONSTRAINT DF_VoucherPackage_Description DEFAULT (N''),
    DepartmentType  varchar(30) NOT NULL,
    Amount          decimal(12,2) NOT NULL,
    ValidDays       int NOT NULL CONSTRAINT DF_VoucherPackage_ValidDays DEFAULT (365),
    IsActive        bit NOT NULL CONSTRAINT DF_VoucherPackage_IsActive DEFAULT (1),
    CreatedUtc      datetime2(0) NOT NULL CONSTRAINT DF_VoucherPackage_CreatedUtc DEFAULT (SYSUTCDATETIME()),
    CreatedBy       nvarchar(256) NOT NULL,
    UpdatedUtc      datetime2(0) NULL,
    UpdatedBy       nvarchar(256) NULL,
    RowVersion      rowversion NOT NULL,
    CONSTRAINT UQ_VoucherPackage_Name UNIQUE (Name),
    CONSTRAINT CK_VoucherPackage_Department CHECK (DepartmentType IN ('Retail', 'FoodAndBeverage')),
    CONSTRAINT CK_VoucherPackage_Amount CHECK (Amount > 0),
    CONSTRAINT CK_VoucherPackage_ValidDays CHECK (ValidDays BETWEEN 1 AND 3650)
);

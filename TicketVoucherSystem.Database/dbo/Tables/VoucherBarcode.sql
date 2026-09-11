CREATE TABLE dbo.VoucherBarcode
(
    Id              bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_VoucherBarcode PRIMARY KEY,
    Code            varchar(32) NOT NULL,
    DepartmentType  varchar(30) NOT NULL,
    Status          varchar(20) NOT NULL CONSTRAINT DF_VoucherBarcode_Status DEFAULT ('Available'),
    ImportedUtc     datetime2(0) NOT NULL CONSTRAINT DF_VoucherBarcode_ImportedUtc DEFAULT (SYSUTCDATETIME()),
    ImportedBy      nvarchar(256) NOT NULL,
    AssignedUtc     datetime2(0) NULL,
    AssignedBy      nvarchar(256) NULL,
    RowVersion      rowversion NOT NULL,
    CONSTRAINT UQ_VoucherBarcode_Code_Department UNIQUE (Code, DepartmentType),
    CONSTRAINT CK_VoucherBarcode_Department CHECK (DepartmentType IN ('Retail', 'FoodAndBeverage')),
    CONSTRAINT CK_VoucherBarcode_Status CHECK (Status IN ('Available', 'Assigned', 'Retired'))
);
GO
CREATE INDEX IX_VoucherBarcode_Allocation
    ON dbo.VoucherBarcode (DepartmentType, Status, ImportedUtc, Id)
    INCLUDE (Code);

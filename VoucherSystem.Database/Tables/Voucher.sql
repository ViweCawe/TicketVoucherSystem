CREATE TABLE dbo.Voucher
(
    Id              bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Voucher PRIMARY KEY,
    PackageId       int NOT NULL,
    Code            varchar(32) NOT NULL,
    DepartmentType  varchar(30) NOT NULL,
    Amount          decimal(12,2) NOT NULL,
    Status          varchar(20) NOT NULL CONSTRAINT DF_Voucher_Status DEFAULT ('Issued'),
    PairReference   varchar(32) NULL,
    IssuedUtc       datetime2(0) NOT NULL CONSTRAINT DF_Voucher_IssuedUtc DEFAULT (SYSUTCDATETIME()),
    IssuedBy        nvarchar(256) NOT NULL,
    ExpiresUtc      datetime2(0) NOT NULL,
    RedeemedUtc     datetime2(0) NULL,
    RedeemedAt      nvarchar(120) NULL,
    RedeemedBy      nvarchar(256) NULL,
    CancelledUtc    datetime2(0) NULL,
    CancelledBy     nvarchar(256) NULL,
    RowVersion      rowversion NOT NULL,
    CONSTRAINT FK_Voucher_Package FOREIGN KEY (PackageId) REFERENCES dbo.VoucherPackage(Id),
    CONSTRAINT UQ_Voucher_Code_Department UNIQUE (Code, DepartmentType),
    CONSTRAINT CK_Voucher_Department CHECK (DepartmentType IN ('Retail', 'FoodAndBeverage')),
    CONSTRAINT CK_Voucher_Status CHECK (Status IN ('Issued', 'Redeemed', 'Expired', 'Cancelled')),
    CONSTRAINT CK_Voucher_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Voucher_Dates CHECK (ExpiresUtc > IssuedUtc)
);

CREATE INDEX IX_Voucher_Status_IssuedUtc
    ON dbo.Voucher (Status, IssuedUtc DESC)
    INCLUDE (Code, PackageId, DepartmentType, Amount, ExpiresUtc, RedeemedUtc);

CREATE INDEX IX_Voucher_PackageId
    ON dbo.Voucher (PackageId);

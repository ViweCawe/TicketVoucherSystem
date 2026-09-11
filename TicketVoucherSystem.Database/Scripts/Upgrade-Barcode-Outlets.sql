SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.Outlet', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Outlet
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Outlet PRIMARY KEY,
        Name nvarchar(120) NOT NULL CONSTRAINT UQ_Outlet_Name UNIQUE,
        Code varchar(20) NOT NULL CONSTRAINT UQ_Outlet_Code UNIQUE,
        IsActive bit NOT NULL CONSTRAINT DF_Outlet_IsActive DEFAULT (1),
        DisplayOrder int NOT NULL CONSTRAINT DF_Outlet_DisplayOrder DEFAULT (0),
        CreatedUtc datetime2(0) NOT NULL CONSTRAINT DF_Outlet_CreatedUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT CK_Outlet_DisplayOrder CHECK (DisplayOrder >= 0)
    );
END;

IF OBJECT_ID('dbo.VoucherBarcode', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VoucherBarcode
    (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_VoucherBarcode PRIMARY KEY,
        Code varchar(32) NOT NULL,
        DepartmentType varchar(30) NOT NULL,
        Status varchar(20) NOT NULL CONSTRAINT DF_VoucherBarcode_Status DEFAULT ('Available'),
        ImportedUtc datetime2(0) NOT NULL CONSTRAINT DF_VoucherBarcode_ImportedUtc DEFAULT (SYSUTCDATETIME()),
        ImportedBy nvarchar(256) NOT NULL,
        AssignedUtc datetime2(0) NULL,
        AssignedBy nvarchar(256) NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT UQ_VoucherBarcode_Code_Department UNIQUE (Code, DepartmentType),
        CONSTRAINT CK_VoucherBarcode_Department CHECK (DepartmentType IN ('Retail', 'FoodAndBeverage')),
        CONSTRAINT CK_VoucherBarcode_Status CHECK (Status IN ('Available', 'Assigned', 'Retired'))
    );
END;

IF COL_LENGTH('dbo.Voucher', 'BarcodeId') IS NULL
    ALTER TABLE dbo.Voucher ADD BarcodeId bigint NULL;
IF COL_LENGTH('dbo.Voucher', 'RedeemedOutletId') IS NULL
    ALTER TABLE dbo.Voucher ADD RedeemedOutletId int NULL;

IF NOT EXISTS (SELECT 1 FROM dbo.Outlet)
BEGIN
    INSERT dbo.Outlet (Name, Code, DisplayOrder)
    VALUES (N'Vista', 'VISTA', 10), (N'Upper Cableway', 'UPPER', 20),
           (N'Lower Cableway', 'LOWER', 30), (N'Table Mountain Cafe', 'CAFE', 40),
           (N'Retail Shop', 'RETAIL', 50);
END;

INSERT dbo.VoucherBarcode (Code, DepartmentType, Status, ImportedUtc, ImportedBy, AssignedUtc, AssignedBy)
SELECT v.Code, v.DepartmentType, 'Assigned', MIN(v.IssuedUtc), MIN(v.IssuedBy), MIN(v.IssuedUtc), MIN(v.IssuedBy)
FROM dbo.Voucher v
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.VoucherBarcode barcode
    WHERE barcode.Code = v.Code AND barcode.DepartmentType = v.DepartmentType
)
GROUP BY v.Code, v.DepartmentType;

UPDATE voucher
SET BarcodeId = barcode.Id
FROM dbo.Voucher voucher
INNER JOIN dbo.VoucherBarcode barcode
    ON barcode.Code = voucher.Code AND barcode.DepartmentType = voucher.DepartmentType
WHERE voucher.BarcodeId IS NULL;

UPDATE voucher
SET RedeemedOutletId = outlet.Id
FROM dbo.Voucher voucher
INNER JOIN dbo.Outlet outlet ON outlet.Name = voucher.RedeemedAt
WHERE voucher.RedeemedOutletId IS NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Voucher_Barcode')
    ALTER TABLE dbo.Voucher ADD CONSTRAINT FK_Voucher_Barcode FOREIGN KEY (BarcodeId) REFERENCES dbo.VoucherBarcode(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Voucher_RedeemedOutlet')
    ALTER TABLE dbo.Voucher ADD CONSTRAINT FK_Voucher_RedeemedOutlet FOREIGN KEY (RedeemedOutletId) REFERENCES dbo.Outlet(Id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Voucher') AND name = 'UX_Voucher_BarcodeId')
    CREATE UNIQUE INDEX UX_Voucher_BarcodeId ON dbo.Voucher (BarcodeId) WHERE BarcodeId IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Voucher') AND name = 'IX_Voucher_RedeemedOutletId')
    CREATE INDEX IX_Voucher_RedeemedOutletId ON dbo.Voucher (RedeemedOutletId, RedeemedUtc DESC) INCLUDE (Amount, DepartmentType);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.VoucherBarcode') AND name = 'IX_VoucherBarcode_Allocation')
    CREATE INDEX IX_VoucherBarcode_Allocation ON dbo.VoucherBarcode (DepartmentType, Status, ImportedUtc, Id) INCLUDE (Code);

COMMIT TRANSACTION;
GO

CREATE OR ALTER VIEW dbo.vVoucherDetails
AS
    SELECT v.Id, v.PackageId, v.BarcodeId, p.Name AS PackageName, v.Code, v.DepartmentType, v.Amount,
           CASE WHEN v.Status = 'Issued' AND v.ExpiresUtc <= SYSUTCDATETIME() THEN 'Expired' ELSE v.Status END AS Status,
           v.PairReference, v.IssuedUtc, v.IssuedBy, v.ExpiresUtc, v.RedeemedUtc, v.RedeemedOutletId,
           v.RedeemedAt, v.RedeemedBy
    FROM dbo.Voucher v
    INNER JOIN dbo.VoucherPackage p ON p.Id = v.PackageId;
GO

CREATE OR ALTER PROCEDURE dbo.spVoucherBarcode_Import
    @CodesJson nvarchar(max), @UserName nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @Codes table (Code varchar(10) PRIMARY KEY);
    INSERT @Codes SELECT DISTINCT CONVERT(varchar(10), [value]) FROM OPENJSON(@CodesJson) WHERE [type] = 1;
    IF NOT EXISTS (SELECT 1 FROM @Codes) THROW 50030, 'No barcodes were supplied.', 1;
    IF EXISTS (SELECT 1 FROM @Codes WHERE Code LIKE '%[^0-9]%' OR LEN(Code) <> 10) THROW 50031, 'Every barcode must contain exactly 10 digits.', 1;

    BEGIN TRANSACTION;
    DECLARE @NewCodes table (Code varchar(10) PRIMARY KEY);
    INSERT @NewCodes
    SELECT codes.Code FROM @Codes codes
    WHERE NOT EXISTS (SELECT 1 FROM dbo.VoucherBarcode barcode WITH (UPDLOCK, HOLDLOCK) WHERE barcode.Code = codes.Code)
      AND NOT EXISTS (SELECT 1 FROM dbo.Voucher voucher WHERE voucher.Code = codes.Code);

    INSERT dbo.VoucherBarcode (Code, DepartmentType, ImportedBy)
    SELECT Code, department.DepartmentType, @UserName
    FROM @NewCodes CROSS JOIN (VALUES ('Retail'), ('FoodAndBeverage')) department(DepartmentType);
    COMMIT TRANSACTION;

    SELECT (SELECT COUNT(*) FROM @NewCodes) AS ImportedCount,
           (SELECT COUNT(*) FROM @Codes) - (SELECT COUNT(*) FROM @NewCodes) AS DuplicateCount,
           (SELECT COUNT(*) FROM dbo.VoucherBarcode WHERE Status = 'Available') AS AvailableCount;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spVoucherBarcode_GetStock
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(CASE WHEN DepartmentType = 'Retail' AND Status = 'Available' THEN 1 END) AS AvailableRetail,
           COUNT(CASE WHEN DepartmentType = 'FoodAndBeverage' AND Status = 'Available' THEN 1 END) AS AvailableFoodAndBeverage,
           COUNT(CASE WHEN DepartmentType = 'Retail' AND Status = 'Assigned' THEN 1 END) AS AssignedRetail,
           COUNT(CASE WHEN DepartmentType = 'FoodAndBeverage' AND Status = 'Assigned' THEN 1 END) AS AssignedFoodAndBeverage
    FROM dbo.VoucherBarcode;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spVoucherBarcode_Search
    @Search nvarchar(100) = NULL, @Status varchar(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (500) Id, Code, DepartmentType, Status, ImportedUtc, ImportedBy, AssignedUtc, AssignedBy
    FROM dbo.VoucherBarcode
    WHERE (@Status IS NULL OR @Status = '' OR Status = @Status)
      AND (@Search IS NULL OR @Search = '' OR Code LIKE '%' + @Search + '%')
    ORDER BY ImportedUtc DESC, Id DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spVoucher_IssueBatch
    @PackageId int, @Quantity int, @UserName nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @Quantity NOT BETWEEN 1 AND 100 THROW 50020, 'Quantity must be between 1 and 100.', 1;
    DECLARE @DepartmentType varchar(30), @Amount decimal(12,2), @ValidDays int;
    SELECT @DepartmentType = DepartmentType, @Amount = Amount, @ValidDays = ValidDays FROM dbo.VoucherPackage WHERE Id = @PackageId AND IsActive = 1;
    IF @DepartmentType IS NULL THROW 50021, 'The selected voucher package is not available.', 1;

    DECLARE @Selected table (BarcodeId bigint PRIMARY KEY, Code varchar(32) NOT NULL);
    DECLARE @Created table (Id bigint PRIMARY KEY, BarcodeId bigint NOT NULL);
    BEGIN TRANSACTION;
    INSERT @Selected SELECT TOP (@Quantity) Id, Code FROM dbo.VoucherBarcode WITH (UPDLOCK, READPAST, ROWLOCK)
    WHERE DepartmentType = @DepartmentType AND Status = 'Available' ORDER BY ImportedUtc, Id;
    IF (SELECT COUNT(*) FROM @Selected) <> @Quantity
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50022, 'There are not enough available barcodes for this voucher type. Import barcode stock first.', 1;
    END;

    INSERT dbo.Voucher (PackageId, BarcodeId, Code, DepartmentType, Amount, IssuedBy, ExpiresUtc)
    OUTPUT inserted.Id, inserted.BarcodeId INTO @Created
    SELECT @PackageId, BarcodeId, Code, @DepartmentType, @Amount, @UserName, DATEADD(day, @ValidDays, SYSUTCDATETIME()) FROM @Selected;
    UPDATE barcode SET Status = 'Assigned', AssignedUtc = SYSUTCDATETIME(), AssignedBy = @UserName
    FROM dbo.VoucherBarcode barcode INNER JOIN @Created created ON created.BarcodeId = barcode.Id;
    INSERT dbo.VoucherEvent (VoucherId, EventType, PerformedBy, Notes)
    SELECT Id, 'Issued', @UserName, 'Voucher allocated from existing barcode inventory.' FROM @Created;
    COMMIT TRANSACTION;

    SELECT details.* FROM dbo.vVoucherDetails details INNER JOIN @Created created ON created.Id = details.Id ORDER BY details.Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spOutlet_GetActive
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Code, IsActive, DisplayOrder FROM dbo.Outlet WHERE IsActive = 1 ORDER BY DisplayOrder, Name;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spOutlet_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Code, IsActive, DisplayOrder FROM dbo.Outlet ORDER BY DisplayOrder, Name;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spOutlet_Save
    @Id int = NULL, @Name nvarchar(120), @Code varchar(20), @DisplayOrder int
AS
BEGIN
    SET NOCOUNT ON;
    IF NULLIF(LTRIM(RTRIM(@Name)), '') IS NULL OR NULLIF(LTRIM(RTRIM(@Code)), '') IS NULL THROW 50060, 'Outlet name and code are required.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Outlet WHERE Id <> COALESCE(@Id, -1) AND (Name = LTRIM(RTRIM(@Name)) OR Code = UPPER(LTRIM(RTRIM(@Code)))))
        THROW 50062, 'Another outlet already uses this name or code.', 1;
    IF @Id IS NULL INSERT dbo.Outlet (Name, Code, DisplayOrder) VALUES (LTRIM(RTRIM(@Name)), UPPER(LTRIM(RTRIM(@Code))), @DisplayOrder);
    ELSE IF EXISTS (SELECT 1 FROM dbo.Outlet WHERE Id = @Id) UPDATE dbo.Outlet SET Name = LTRIM(RTRIM(@Name)), Code = UPPER(LTRIM(RTRIM(@Code))), DisplayOrder = @DisplayOrder WHERE Id = @Id;
    ELSE THROW 50061, 'Outlet not found.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spOutlet_SetActive @Id int, @IsActive bit
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Outlet SET IsActive = @IsActive WHERE Id = @Id;
    IF @@ROWCOUNT = 0 THROW 50061, 'Outlet not found.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spVoucher_Redeem
    @Code varchar(32), @DepartmentType varchar(30), @OutletId int, @UserName nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @VoucherId bigint, @Status varchar(20), @ExpiresUtc datetime2(0), @OutletName nvarchar(120);
    SELECT @OutletName = Name FROM dbo.Outlet WHERE Id = @OutletId AND IsActive = 1;
    IF @OutletName IS NULL THROW 50006, 'Select an active redemption outlet.', 1;
    BEGIN TRANSACTION;
    SELECT @VoucherId = Id, @Status = Status, @ExpiresUtc = ExpiresUtc FROM dbo.Voucher WITH (UPDLOCK, HOLDLOCK)
    WHERE Code = @Code AND DepartmentType = @DepartmentType;
    IF @VoucherId IS NULL BEGIN ROLLBACK; THROW 50001, 'Voucher not found for this department.', 1; END;
    IF @Status = 'Redeemed' BEGIN ROLLBACK; THROW 50002, 'This voucher has already been redeemed.', 1; END;
    IF @Status = 'Cancelled' BEGIN ROLLBACK; THROW 50003, 'This voucher was cancelled.', 1; END;
    IF @Status = 'Expired' OR @ExpiresUtc <= SYSUTCDATETIME()
    BEGIN
        IF @Status <> 'Expired'
        BEGIN
            UPDATE dbo.Voucher SET Status = 'Expired' WHERE Id = @VoucherId;
            INSERT dbo.VoucherEvent (VoucherId, EventType, PerformedBy, Location, Notes)
            VALUES (@VoucherId, 'Expired', @UserName, @OutletName, 'Redemption rejected because the voucher had expired.');
        END;
        COMMIT; THROW 50004, 'This voucher has expired.', 1;
    END;
    UPDATE dbo.Voucher SET Status = 'Redeemed', RedeemedUtc = SYSUTCDATETIME(), RedeemedOutletId = @OutletId,
        RedeemedAt = @OutletName, RedeemedBy = @UserName WHERE Id = @VoucherId AND Status = 'Issued';
    IF @@ROWCOUNT <> 1 BEGIN ROLLBACK; THROW 50005, 'The voucher could not be redeemed.', 1; END;
    INSERT dbo.VoucherEvent (VoucherId, EventType, PerformedBy, Location) VALUES (@VoucherId, 'Redeemed', @UserName, @OutletName);
    COMMIT;
    SELECT * FROM dbo.vVoucherDetails WHERE Id = @VoucherId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spVoucher_ImportPairs
    @CodesJson nvarchar(max), @RetailPackageId int, @FoodPackageId int, @UserName nvarchar(256)
AS
BEGIN
    THROW 50034, 'Direct voucher creation from import is disabled. Import barcode stock, then issue vouchers from the allocation page.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spReport_GetOutletPerformance @Days int = 30
AS
BEGIN
    SET NOCOUNT ON;
    SET @Days = CASE WHEN @Days IN (7,30,90) THEN @Days ELSE 30 END;
    DECLARE @FromUtc datetime2(0) = DATEADD(day, 1 - @Days, CONVERT(date, SYSUTCDATETIME()));
    DECLARE @Total decimal(12,2) = (SELECT COALESCE(SUM(Amount),0) FROM dbo.Voucher WHERE Status = 'Redeemed' AND RedeemedUtc >= @FromUtc);
    SELECT v.RedeemedOutletId AS OutletId, COALESCE(outlet.Name, NULLIF(v.RedeemedAt,''), 'Unknown outlet') AS OutletName,
           COUNT(*) AS RedeemedCount, SUM(v.Amount) AS RedeemedValue,
           CAST(CASE WHEN @Total = 0 THEN 0 ELSE 100.0 * SUM(v.Amount) / @Total END AS decimal(5,1)) AS SharePercent
    FROM dbo.Voucher v LEFT JOIN dbo.Outlet outlet ON outlet.Id = v.RedeemedOutletId
    WHERE v.Status = 'Redeemed' AND v.RedeemedUtc >= @FromUtc
    GROUP BY v.RedeemedOutletId, COALESCE(outlet.Name, NULLIF(v.RedeemedAt,''), 'Unknown outlet')
    ORDER BY RedeemedValue DESC, OutletName;
END;
GO

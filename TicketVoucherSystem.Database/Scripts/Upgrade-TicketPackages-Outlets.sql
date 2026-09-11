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

IF OBJECT_ID('dbo.TicketPackage', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TicketPackage
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TicketPackage PRIMARY KEY,
        Name nvarchar(80) NOT NULL,
        Description nvarchar(200) NOT NULL CONSTRAINT DF_TicketPackage_Description DEFAULT (N''),
        IsActive bit NOT NULL CONSTRAINT DF_TicketPackage_IsActive DEFAULT (1),
        CreatedUtc datetime2(0) NOT NULL CONSTRAINT DF_TicketPackage_CreatedUtc DEFAULT (SYSUTCDATETIME()),
        CreatedBy nvarchar(256) NOT NULL,
        CONSTRAINT UQ_TicketPackage_Name UNIQUE (Name)
    );
END;

IF OBJECT_ID('dbo.TicketPackageBenefit', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TicketPackageBenefit
    (
        TicketPackageId int NOT NULL,
        VoucherPackageId int NOT NULL,
        DisplayOrder int NOT NULL CONSTRAINT DF_TicketPackageBenefit_DisplayOrder DEFAULT (0),
        CONSTRAINT PK_TicketPackageBenefit PRIMARY KEY (TicketPackageId, VoucherPackageId),
        CONSTRAINT FK_TicketPackageBenefit_TicketPackage FOREIGN KEY (TicketPackageId) REFERENCES dbo.TicketPackage(Id),
        CONSTRAINT FK_TicketPackageBenefit_VoucherPackage FOREIGN KEY (VoucherPackageId) REFERENCES dbo.VoucherPackage(Id)
    );
END;

IF OBJECT_ID('dbo.TicketVoucherIssue', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TicketVoucherIssue
    (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_TicketVoucherIssue PRIMARY KEY,
        TicketPackageId int NOT NULL,
        TicketNumber varchar(32) NOT NULL,
        IssuedUtc datetime2(0) NOT NULL CONSTRAINT DF_TicketVoucherIssue_IssuedUtc DEFAULT (SYSUTCDATETIME()),
        IssuedBy nvarchar(256) NOT NULL,
        CONSTRAINT FK_TicketVoucherIssue_TicketPackage FOREIGN KEY (TicketPackageId) REFERENCES dbo.TicketPackage(Id),
        CONSTRAINT UQ_TicketVoucherIssue_TicketNumber UNIQUE (TicketNumber)
    );
END;

IF COL_LENGTH('dbo.Voucher', 'TicketIssueId') IS NULL
    ALTER TABLE dbo.Voucher ADD TicketIssueId bigint NULL;

IF COL_LENGTH('dbo.Voucher', 'RedeemedOutletId') IS NULL
    ALTER TABLE dbo.Voucher ADD RedeemedOutletId int NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Voucher_TicketIssue')
    ALTER TABLE dbo.Voucher ADD CONSTRAINT FK_Voucher_TicketIssue FOREIGN KEY (TicketIssueId) REFERENCES dbo.TicketVoucherIssue(Id);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Voucher_RedeemedOutlet')
    ALTER TABLE dbo.Voucher ADD CONSTRAINT FK_Voucher_RedeemedOutlet FOREIGN KEY (RedeemedOutletId) REFERENCES dbo.Outlet(Id);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Voucher') AND name = 'IX_Voucher_RedeemedOutletId')
    CREATE INDEX IX_Voucher_RedeemedOutletId ON dbo.Voucher (RedeemedOutletId, RedeemedUtc DESC) INCLUDE (Amount, DepartmentType);

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

UPDATE voucher
SET RedeemedOutletId = outlet.Id
FROM dbo.Voucher voucher
INNER JOIN dbo.Outlet outlet ON outlet.Name = voucher.RedeemedAt
WHERE voucher.RedeemedOutletId IS NULL;

IF NOT EXISTS (SELECT 1 FROM dbo.VoucherPackage WHERE Name = N'F&B R100')
    INSERT dbo.VoucherPackage (Name, Description, DepartmentType, Amount, ValidDays, CreatedBy)
    VALUES (N'F&B R100', N'R100 off food and beverage purchases', 'FoodAndBeverage', 100.00, 365, N'Seed');

IF NOT EXISTS (SELECT 1 FROM dbo.VoucherPackage WHERE Name = N'Retail R250')
    INSERT dbo.VoucherPackage (Name, Description, DepartmentType, Amount, ValidDays, CreatedBy)
    VALUES (N'Retail R250', N'R250 off retail purchases', 'Retail', 250.00, 365, N'Seed');

IF NOT EXISTS (SELECT 1 FROM dbo.TicketPackage WHERE Name = N'VIP Ticket')
    INSERT dbo.TicketPackage (Name, Description, CreatedBy)
    VALUES (N'VIP Ticket', N'Fast-track ticket with F&B and retail voucher benefits', N'Seed');

DECLARE @VipTicketPackageId int = (SELECT TOP (1) Id FROM dbo.TicketPackage WHERE Name = N'VIP Ticket');
DECLARE @FnbBenefitId int = (SELECT TOP (1) Id FROM dbo.VoucherPackage WHERE Name = N'F&B R100');
DECLARE @RetailBenefitId int = (SELECT TOP (1) Id FROM dbo.VoucherPackage WHERE Name = N'Retail R250');

IF NOT EXISTS (SELECT 1 FROM dbo.TicketPackageBenefit WHERE TicketPackageId = @VipTicketPackageId AND VoucherPackageId = @FnbBenefitId)
    INSERT dbo.TicketPackageBenefit (TicketPackageId, VoucherPackageId, DisplayOrder) VALUES (@VipTicketPackageId, @FnbBenefitId, 10);

IF NOT EXISTS (SELECT 1 FROM dbo.TicketPackageBenefit WHERE TicketPackageId = @VipTicketPackageId AND VoucherPackageId = @RetailBenefitId)
    INSERT dbo.TicketPackageBenefit (TicketPackageId, VoucherPackageId, DisplayOrder) VALUES (@VipTicketPackageId, @RetailBenefitId, 20);

COMMIT TRANSACTION;
GO

CREATE OR ALTER VIEW dbo.vVoucherDetails
AS
    SELECT v.Id, v.PackageId, v.TicketIssueId, p.Name AS PackageName, v.Code, v.DepartmentType, v.Amount,
           CASE WHEN v.Status = 'Issued' AND v.ExpiresUtc <= SYSUTCDATETIME() THEN 'Expired' ELSE v.Status END AS Status,
           v.PairReference, v.IssuedUtc, v.IssuedBy, v.ExpiresUtc, v.RedeemedUtc, v.RedeemedOutletId,
           v.RedeemedAt, v.RedeemedBy
    FROM dbo.Voucher v
    INNER JOIN dbo.VoucherPackage p ON p.Id = v.PackageId;
GO

CREATE OR ALTER PROCEDURE dbo.spTicketPackage_GetActive
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ticketPackage.Id, ticketPackage.Name, ticketPackage.Description,
           STRING_AGG(CONCAT(voucherPackage.Name, ' - R', CONVERT(varchar(20), voucherPackage.Amount)), ', ')
               WITHIN GROUP (ORDER BY benefit.DisplayOrder, voucherPackage.Name) AS BenefitSummary,
           COUNT(*) AS BenefitCount
    FROM dbo.TicketPackage ticketPackage
    INNER JOIN dbo.TicketPackageBenefit benefit ON benefit.TicketPackageId = ticketPackage.Id
    INNER JOIN dbo.VoucherPackage voucherPackage ON voucherPackage.Id = benefit.VoucherPackageId
    WHERE ticketPackage.IsActive = 1 AND voucherPackage.IsActive = 1
    GROUP BY ticketPackage.Id, ticketPackage.Name, ticketPackage.Description
    ORDER BY ticketPackage.Name;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spTicketVoucher_Issue
    @TicketNumber varchar(32), @TicketPackageId int, @UserName nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @TicketNumber = LTRIM(RTRIM(@TicketNumber));

    IF LEN(@TicketNumber) < 6 OR LEN(@TicketNumber) > 32 OR @TicketNumber LIKE '%[^0-9A-Za-z-]%'
        THROW 50020, 'Enter a valid ticket barcode containing 6 to 32 letters, numbers, or hyphens.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.TicketPackage WHERE Id = @TicketPackageId AND IsActive = 1)
        THROW 50021, 'The selected ticket package is not available.', 1;
    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.TicketPackageBenefit benefit
        INNER JOIN dbo.VoucherPackage voucherPackage ON voucherPackage.Id = benefit.VoucherPackageId
        WHERE benefit.TicketPackageId = @TicketPackageId AND voucherPackage.IsActive = 1
    )
        THROW 50022, 'The selected ticket package has no active voucher benefits.', 1;
    IF EXISTS
    (
        SELECT voucherPackage.DepartmentType
        FROM dbo.TicketPackageBenefit benefit
        INNER JOIN dbo.VoucherPackage voucherPackage ON voucherPackage.Id = benefit.VoucherPackageId
        WHERE benefit.TicketPackageId = @TicketPackageId AND voucherPackage.IsActive = 1
        GROUP BY voucherPackage.DepartmentType HAVING COUNT(*) > 1
    )
        THROW 50023, 'A ticket package may contain only one voucher benefit for each department.', 1;

    DECLARE @TicketIssueId bigint;
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM dbo.TicketVoucherIssue WITH (UPDLOCK, HOLDLOCK) WHERE TicketNumber = @TicketNumber)
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50024, 'Voucher benefits have already been attached to this ticket.', 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.Voucher WITH (UPDLOCK, HOLDLOCK) WHERE Code = @TicketNumber)
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50024, 'Voucher benefits have already been attached to this ticket.', 1;
    END;

    INSERT dbo.TicketVoucherIssue (TicketPackageId, TicketNumber, IssuedBy)
    VALUES (@TicketPackageId, @TicketNumber, @UserName);
    SET @TicketIssueId = SCOPE_IDENTITY();

    DECLARE @Created table (Id bigint PRIMARY KEY);
    INSERT dbo.Voucher (TicketIssueId, PackageId, Code, DepartmentType, Amount, PairReference, IssuedBy, ExpiresUtc)
    OUTPUT inserted.Id INTO @Created(Id)
    SELECT @TicketIssueId, voucherPackage.Id, @TicketNumber, voucherPackage.DepartmentType,
           voucherPackage.Amount, @TicketNumber, @UserName, DATEADD(day, voucherPackage.ValidDays, SYSUTCDATETIME())
    FROM dbo.TicketPackageBenefit benefit
    INNER JOIN dbo.VoucherPackage voucherPackage ON voucherPackage.Id = benefit.VoucherPackageId
    WHERE benefit.TicketPackageId = @TicketPackageId AND voucherPackage.IsActive = 1;

    INSERT dbo.VoucherEvent (VoucherId, EventType, PerformedBy, Notes)
    SELECT Id, 'Issued', @UserName, 'Voucher benefit attached to an existing ticket barcode.' FROM @Created;
    COMMIT TRANSACTION;

    SELECT details.* FROM dbo.vVoucherDetails details
    INNER JOIN @Created created ON created.Id = details.Id
    ORDER BY details.DepartmentType;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spTicketVoucherIssue_GetById @Id bigint
AS
BEGIN
    SET NOCOUNT ON;
    SELECT issue.Id, issue.TicketPackageId, ticketPackage.Name AS PackageName,
           ticketPackage.Description AS PackageDescription, issue.TicketNumber, issue.IssuedUtc, issue.IssuedBy
    FROM dbo.TicketVoucherIssue issue
    INNER JOIN dbo.TicketPackage ticketPackage ON ticketPackage.Id = issue.TicketPackageId
    WHERE issue.Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spTicketVoucherIssue_GetVouchers @Id bigint
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vVoucherDetails WHERE TicketIssueId = @Id ORDER BY DepartmentType;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spVoucher_IssueBatch @PackageId int, @Quantity int, @UserName nvarchar(256)
AS
BEGIN
    THROW 50025, 'Batch-generated voucher codes are disabled. Attach a ticket package to an existing ticket barcode.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spVoucher_ImportPairs
    @CodesJson nvarchar(max), @RetailPackageId int, @FoodPackageId int, @UserName nvarchar(256)
AS
BEGIN
    THROW 50034, 'Barcode import is disabled. Scan the existing ticket barcode on the Issue package page.', 1;
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
    IF NULLIF(LTRIM(RTRIM(@Name)), '') IS NULL OR NULLIF(LTRIM(RTRIM(@Code)), '') IS NULL
        THROW 50060, 'Outlet name and code are required.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Outlet WHERE Id <> COALESCE(@Id, -1) AND (Name = LTRIM(RTRIM(@Name)) OR Code = UPPER(LTRIM(RTRIM(@Code)))))
        THROW 50062, 'Another outlet already uses this name or code.', 1;
    IF @Id IS NULL
        INSERT dbo.Outlet (Name, Code, DisplayOrder) VALUES (LTRIM(RTRIM(@Name)), UPPER(LTRIM(RTRIM(@Code))), @DisplayOrder);
    ELSE IF EXISTS (SELECT 1 FROM dbo.Outlet WHERE Id = @Id)
        UPDATE dbo.Outlet SET Name = LTRIM(RTRIM(@Name)), Code = UPPER(LTRIM(RTRIM(@Code))), DisplayOrder = @DisplayOrder WHERE Id = @Id;
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
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @VoucherId bigint, @Status varchar(20), @ExpiresUtc datetime2(0), @OutletName nvarchar(120);
    SELECT @OutletName = Name FROM dbo.Outlet WHERE Id = @OutletId AND IsActive = 1;
    IF @OutletName IS NULL THROW 50006, 'Select an active redemption outlet.', 1;

    BEGIN TRANSACTION;
    SELECT @VoucherId = Id, @Status = Status, @ExpiresUtc = ExpiresUtc
    FROM dbo.Voucher WITH (UPDLOCK, HOLDLOCK)
    WHERE Code = @Code AND DepartmentType = @DepartmentType;
    IF @VoucherId IS NULL BEGIN ROLLBACK TRANSACTION; THROW 50001, 'Voucher not found for this department.', 1; END;
    IF @Status = 'Redeemed' BEGIN ROLLBACK TRANSACTION; THROW 50002, 'This voucher has already been redeemed.', 1; END;
    IF @Status = 'Cancelled' BEGIN ROLLBACK TRANSACTION; THROW 50003, 'This voucher was cancelled.', 1; END;
    IF @Status = 'Expired' OR @ExpiresUtc <= SYSUTCDATETIME()
    BEGIN
        IF @Status <> 'Expired'
        BEGIN
            UPDATE dbo.Voucher SET Status = 'Expired' WHERE Id = @VoucherId;
            INSERT dbo.VoucherEvent (VoucherId, EventType, PerformedBy, Location, Notes)
            VALUES (@VoucherId, 'Expired', @UserName, @OutletName, 'Redemption rejected because the voucher had expired.');
        END;
        COMMIT TRANSACTION;
        THROW 50004, 'This voucher has expired.', 1;
    END;

    UPDATE dbo.Voucher
    SET Status = 'Redeemed', RedeemedUtc = SYSUTCDATETIME(), RedeemedOutletId = @OutletId,
        RedeemedAt = @OutletName, RedeemedBy = @UserName
    WHERE Id = @VoucherId AND Status = 'Issued';
    IF @@ROWCOUNT <> 1 BEGIN ROLLBACK TRANSACTION; THROW 50005, 'The voucher could not be redeemed.', 1; END;
    INSERT dbo.VoucherEvent (VoucherId, EventType, PerformedBy, Location)
    VALUES (@VoucherId, 'Redeemed', @UserName, @OutletName);
    COMMIT TRANSACTION;
    SELECT * FROM dbo.vVoucherDetails WHERE Id = @VoucherId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spReport_GetExecutiveSummary @Days int = 30
AS
BEGIN
    SET NOCOUNT ON;
    SET @Days = CASE WHEN @Days IN (7, 30, 90) THEN @Days ELSE 30 END;
    DECLARE @FromUtc datetime2(0) = DATEADD(day, 1 - @Days, CONVERT(date, SYSUTCDATETIME()));
    SELECT COUNT(*) AS TotalIssued, COUNT(CASE WHEN Status = 'Redeemed' THEN 1 END) AS TotalRedeemed,
           COUNT(CASE WHEN Status = 'Issued' THEN 1 END) AS TotalOutstanding, COALESCE(SUM(Amount), 0) AS IssuedValue,
           COALESCE(SUM(CASE WHEN Status = 'Redeemed' THEN Amount ELSE 0 END), 0) AS RedeemedValue,
           COALESCE(SUM(CASE WHEN Status = 'Issued' THEN Amount ELSE 0 END), 0) AS OutstandingValue,
           CAST(CASE WHEN COUNT(*) = 0 THEN 0 ELSE 100.0 * COUNT(CASE WHEN Status = 'Redeemed' THEN 1 END) / COUNT(*) END AS decimal(5, 1)) AS RedemptionRate,
           COUNT(CASE WHEN CONVERT(date, IssuedUtc) = CONVERT(date, SYSUTCDATETIME()) THEN 1 END) AS IssuedToday,
           COUNT(CASE WHEN CONVERT(date, RedeemedUtc) = CONVERT(date, SYSUTCDATETIME()) THEN 1 END) AS RedeemedToday
    FROM dbo.vVoucherDetails WHERE IssuedUtc >= @FromUtc;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spReport_GetVoucherTrend @Days int = 30
AS
BEGIN
    SET NOCOUNT ON;
    SET @Days = CASE WHEN @Days IN (7, 30, 90) THEN @Days ELSE 30 END;
    DECLARE @FromDate date = DATEADD(day, 1 - @Days, CONVERT(date, SYSUTCDATETIME()));
    ;WITH Dates AS
    (
        SELECT @FromDate AS ReportDate
        UNION ALL SELECT DATEADD(day, 1, ReportDate) FROM Dates WHERE ReportDate < CONVERT(date, SYSUTCDATETIME())
    )
    SELECT dates.ReportDate, COALESCE(issued.IssuedCount, 0) AS IssuedCount,
           COALESCE(redeemed.RedeemedCount, 0) AS RedeemedCount, COALESCE(issued.IssuedValue, 0) AS IssuedValue,
           COALESCE(redeemed.RedeemedValue, 0) AS RedeemedValue
    FROM Dates dates
    OUTER APPLY (SELECT COUNT(*) AS IssuedCount, COALESCE(SUM(Amount), 0) AS IssuedValue FROM dbo.vVoucherDetails
                 WHERE IssuedUtc >= dates.ReportDate AND IssuedUtc < DATEADD(day, 1, dates.ReportDate)) issued
    OUTER APPLY (SELECT COUNT(*) AS RedeemedCount, COALESCE(SUM(Amount), 0) AS RedeemedValue FROM dbo.vVoucherDetails
                 WHERE RedeemedUtc >= dates.ReportDate AND RedeemedUtc < DATEADD(day, 1, dates.ReportDate)) redeemed
    ORDER BY dates.ReportDate OPTION (MAXRECURSION 100);
END;
GO

CREATE OR ALTER PROCEDURE dbo.spReport_GetDepartmentPerformance @Days int = 30
AS
BEGIN
    SET NOCOUNT ON;
    SET @Days = CASE WHEN @Days IN (7, 30, 90) THEN @Days ELSE 30 END;
    DECLARE @FromUtc datetime2(0) = DATEADD(day, 1 - @Days, CONVERT(date, SYSUTCDATETIME()));
    SELECT DepartmentType, COUNT(*) AS IssuedCount, COUNT(CASE WHEN Status = 'Redeemed' THEN 1 END) AS RedeemedCount,
           COUNT(CASE WHEN Status = 'Issued' THEN 1 END) AS OutstandingCount, COALESCE(SUM(Amount), 0) AS IssuedValue,
           COALESCE(SUM(CASE WHEN Status = 'Redeemed' THEN Amount ELSE 0 END), 0) AS RedeemedValue
    FROM dbo.vVoucherDetails WHERE IssuedUtc >= @FromUtc
    GROUP BY DepartmentType ORDER BY RedeemedValue DESC, DepartmentType;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spReport_GetOutletPerformance @Days int = 30
AS
BEGIN
    SET NOCOUNT ON;
    SET @Days = CASE WHEN @Days IN (7, 30, 90) THEN @Days ELSE 30 END;
    DECLARE @FromUtc datetime2(0) = DATEADD(day, 1 - @Days, CONVERT(date, SYSUTCDATETIME()));
    DECLARE @Total decimal(12,2) = (SELECT COALESCE(SUM(Amount), 0) FROM dbo.Voucher WHERE Status = 'Redeemed' AND RedeemedUtc >= @FromUtc);
    SELECT v.RedeemedOutletId AS OutletId, COALESCE(outlet.Name, NULLIF(v.RedeemedAt, ''), 'Unknown outlet') AS OutletName,
           COUNT(*) AS RedeemedCount, SUM(v.Amount) AS RedeemedValue,
           CAST(CASE WHEN @Total = 0 THEN 0 ELSE 100.0 * SUM(v.Amount) / @Total END AS decimal(5,1)) AS SharePercent
    FROM dbo.Voucher v LEFT JOIN dbo.Outlet outlet ON outlet.Id = v.RedeemedOutletId
    WHERE v.Status = 'Redeemed' AND v.RedeemedUtc >= @FromUtc
    GROUP BY v.RedeemedOutletId, COALESCE(outlet.Name, NULLIF(v.RedeemedAt, ''), 'Unknown outlet')
    ORDER BY RedeemedValue DESC, OutletName;
END;
GO

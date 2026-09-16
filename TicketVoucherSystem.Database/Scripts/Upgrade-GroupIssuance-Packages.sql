SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.TicketVoucherIssue', 'BatchId') IS NULL
    ALTER TABLE dbo.TicketVoucherIssue ADD BatchId uniqueidentifier NULL;

UPDATE dbo.TicketVoucherIssue SET BatchId = NEWID() WHERE BatchId IS NULL;

IF EXISTS
(
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.TicketVoucherIssue') AND name = 'BatchId' AND is_nullable = 1
)
    ALTER TABLE dbo.TicketVoucherIssue ALTER COLUMN BatchId uniqueidentifier NOT NULL;

IF NOT EXISTS
(
    SELECT 1 FROM sys.default_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.TicketVoucherIssue') AND name = 'DF_TicketVoucherIssue_BatchId'
)
    ALTER TABLE dbo.TicketVoucherIssue
        ADD CONSTRAINT DF_TicketVoucherIssue_BatchId DEFAULT (NEWSEQUENTIALID()) FOR BatchId;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.TicketVoucherIssue') AND name = 'IX_TicketVoucherIssue_BatchId'
)
    CREATE INDEX IX_TicketVoucherIssue_BatchId ON dbo.TicketVoucherIssue (BatchId, Id);

COMMIT TRANSACTION;
GO

CREATE OR ALTER PROCEDURE dbo.spTicketPackage_GetActive
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ticketPackage.Id, ticketPackage.Name, ticketPackage.Description,
           MAX(CASE WHEN voucherPackage.DepartmentType = 'Retail' THEN voucherPackage.Amount END) AS RetailAmount,
           MAX(CASE WHEN voucherPackage.DepartmentType = 'FoodAndBeverage' THEN voucherPackage.Amount END) AS FoodAndBeverageAmount,
           MAX(voucherPackage.ValidDays) AS ValidDays, ticketPackage.IsActive,
           CONCAT_WS(' + ',
               MAX(CASE WHEN voucherPackage.DepartmentType = 'Retail' THEN CONCAT('Retail R', CONVERT(varchar(32), voucherPackage.Amount)) END),
               MAX(CASE WHEN voucherPackage.DepartmentType = 'FoodAndBeverage' THEN CONCAT('F&B R', CONVERT(varchar(32), voucherPackage.Amount)) END)) AS BenefitSummary,
           COUNT(*) AS BenefitCount
    FROM dbo.TicketPackage ticketPackage
    INNER JOIN dbo.TicketPackageBenefit benefit ON benefit.TicketPackageId = ticketPackage.Id
    INNER JOIN dbo.VoucherPackage voucherPackage ON voucherPackage.Id = benefit.VoucherPackageId
    WHERE ticketPackage.IsActive = 1 AND voucherPackage.IsActive = 1
    GROUP BY ticketPackage.Id, ticketPackage.Name, ticketPackage.Description, ticketPackage.IsActive
    ORDER BY ticketPackage.Name;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spTicketPackage_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ticketPackage.Id, ticketPackage.Name, ticketPackage.Description,
           MAX(CASE WHEN voucherPackage.DepartmentType = 'Retail' THEN voucherPackage.Amount END) AS RetailAmount,
           MAX(CASE WHEN voucherPackage.DepartmentType = 'FoodAndBeverage' THEN voucherPackage.Amount END) AS FoodAndBeverageAmount,
           COALESCE(MAX(voucherPackage.ValidDays), 365) AS ValidDays, ticketPackage.IsActive,
           CONCAT_WS(' + ',
               MAX(CASE WHEN voucherPackage.DepartmentType = 'Retail' THEN CONCAT('Retail R', CONVERT(varchar(32), voucherPackage.Amount)) END),
               MAX(CASE WHEN voucherPackage.DepartmentType = 'FoodAndBeverage' THEN CONCAT('F&B R', CONVERT(varchar(32), voucherPackage.Amount)) END)) AS BenefitSummary,
           COUNT(voucherPackage.Id) AS BenefitCount
    FROM dbo.TicketPackage ticketPackage
    LEFT JOIN dbo.TicketPackageBenefit benefit ON benefit.TicketPackageId = ticketPackage.Id
    LEFT JOIN dbo.VoucherPackage voucherPackage ON voucherPackage.Id = benefit.VoucherPackageId
    GROUP BY ticketPackage.Id, ticketPackage.Name, ticketPackage.Description, ticketPackage.IsActive
    ORDER BY ticketPackage.Name;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spTicketPackage_Save
    @Id int,
    @Name nvarchar(60),
    @Description nvarchar(200),
    @RetailAmount decimal(12,2) = NULL,
    @FoodAndBeverageAmount decimal(12,2) = NULL,
    @ValidDays int,
    @UserName nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @Name = LTRIM(RTRIM(@Name));
    SET @Description = LTRIM(RTRIM(COALESCE(@Description, N'')));

    IF NULLIF(@Name, N'') IS NULL THROW 50040, 'Package name is required.', 1;
    IF @RetailAmount IS NULL AND @FoodAndBeverageAmount IS NULL THROW 50041, 'Add a Retail amount, an F&B amount, or both.', 1;
    IF COALESCE(@RetailAmount, 1) <= 0 OR COALESCE(@FoodAndBeverageAmount, 1) <= 0 OR @ValidDays NOT BETWEEN 1 AND 3650
        THROW 50042, 'Voucher values must be positive and validity must be between 1 and 3650 days.', 1;
    DECLARE @TicketPackageId int = @Id;
    DECLARE @RetailPackageId int;
    DECLARE @FoodPackageId int;
    DECLARE @RetailName nvarchar(80) = CONCAT(@Name, N' | Retail');
    DECLARE @FoodName nvarchar(80) = CONCAT(@Name, N' | F&B');

    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM dbo.TicketPackage WITH (UPDLOCK, HOLDLOCK) WHERE Name = @Name AND Id <> @Id)
    BEGIN ROLLBACK TRANSACTION; THROW 50043, 'Another ticket package already uses this name.', 1; END;
    IF @TicketPackageId = 0
    BEGIN
        INSERT dbo.TicketPackage (Name, Description, CreatedBy) VALUES (@Name, @Description, @UserName);
        SET @TicketPackageId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.TicketPackage SET Name = @Name, Description = @Description WHERE Id = @TicketPackageId;
        IF @@ROWCOUNT = 0 BEGIN ROLLBACK TRANSACTION; THROW 50044, 'Ticket package not found.', 1; END;
    END;

    SELECT TOP (1) @RetailPackageId = voucherPackage.Id
    FROM dbo.TicketPackageBenefit benefit
    INNER JOIN dbo.VoucherPackage voucherPackage ON voucherPackage.Id = benefit.VoucherPackageId
    WHERE benefit.TicketPackageId = @TicketPackageId AND voucherPackage.DepartmentType = 'Retail';
    SELECT TOP (1) @FoodPackageId = voucherPackage.Id
    FROM dbo.TicketPackageBenefit benefit
    INNER JOIN dbo.VoucherPackage voucherPackage ON voucherPackage.Id = benefit.VoucherPackageId
    WHERE benefit.TicketPackageId = @TicketPackageId AND voucherPackage.DepartmentType = 'FoodAndBeverage';

    IF @RetailAmount IS NOT NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.VoucherPackage WITH (UPDLOCK, HOLDLOCK) WHERE Name = @RetailName AND Id <> COALESCE(@RetailPackageId, -1))
        BEGIN ROLLBACK TRANSACTION; THROW 50045, 'The generated Retail benefit name is already in use.', 1; END;
        IF @RetailPackageId IS NULL
        BEGIN
            INSERT dbo.VoucherPackage (Name, Description, DepartmentType, Amount, ValidDays, CreatedBy)
            VALUES (@RetailName, CONCAT(@Name, N' Retail benefit'), 'Retail', @RetailAmount, @ValidDays, @UserName);
            SET @RetailPackageId = SCOPE_IDENTITY();
            INSERT dbo.TicketPackageBenefit (TicketPackageId, VoucherPackageId, DisplayOrder)
            VALUES (@TicketPackageId, @RetailPackageId, 10);
        END
        ELSE
            UPDATE dbo.VoucherPackage
            SET Name = @RetailName, Description = CONCAT(@Name, N' Retail benefit'), Amount = @RetailAmount,
                ValidDays = @ValidDays, IsActive = 1, UpdatedUtc = SYSUTCDATETIME(), UpdatedBy = @UserName
            WHERE Id = @RetailPackageId;
    END
    ELSE IF @RetailPackageId IS NOT NULL
    BEGIN
        DELETE dbo.TicketPackageBenefit WHERE TicketPackageId = @TicketPackageId AND VoucherPackageId = @RetailPackageId;
        UPDATE dbo.VoucherPackage SET IsActive = 0, UpdatedUtc = SYSUTCDATETIME(), UpdatedBy = @UserName WHERE Id = @RetailPackageId;
    END;

    IF @FoodAndBeverageAmount IS NOT NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.VoucherPackage WITH (UPDLOCK, HOLDLOCK) WHERE Name = @FoodName AND Id <> COALESCE(@FoodPackageId, -1))
        BEGIN ROLLBACK TRANSACTION; THROW 50046, 'The generated F&B benefit name is already in use.', 1; END;
        IF @FoodPackageId IS NULL
        BEGIN
            INSERT dbo.VoucherPackage (Name, Description, DepartmentType, Amount, ValidDays, CreatedBy)
            VALUES (@FoodName, CONCAT(@Name, N' F&B benefit'), 'FoodAndBeverage', @FoodAndBeverageAmount, @ValidDays, @UserName);
            SET @FoodPackageId = SCOPE_IDENTITY();
            INSERT dbo.TicketPackageBenefit (TicketPackageId, VoucherPackageId, DisplayOrder)
            VALUES (@TicketPackageId, @FoodPackageId, 20);
        END
        ELSE
            UPDATE dbo.VoucherPackage
            SET Name = @FoodName, Description = CONCAT(@Name, N' F&B benefit'), Amount = @FoodAndBeverageAmount,
                ValidDays = @ValidDays, IsActive = 1, UpdatedUtc = SYSUTCDATETIME(), UpdatedBy = @UserName
            WHERE Id = @FoodPackageId;
    END
    ELSE IF @FoodPackageId IS NOT NULL
    BEGIN
        DELETE dbo.TicketPackageBenefit WHERE TicketPackageId = @TicketPackageId AND VoucherPackageId = @FoodPackageId;
        UPDATE dbo.VoucherPackage SET IsActive = 0, UpdatedUtc = SYSUTCDATETIME(), UpdatedBy = @UserName WHERE Id = @FoodPackageId;
    END;

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spTicketPackage_SetActive
    @Id int, @IsActive bit, @UserName nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.TicketPackage SET IsActive = @IsActive WHERE Id = @Id;
    IF @@ROWCOUNT = 0 THROW 50044, 'Ticket package not found.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spTicketVoucher_IssueBatch
    @TicketNumbersJson nvarchar(max), @TicketPackageId int, @UserName nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF ISJSON(@TicketNumbersJson) <> 1 THROW 50020, 'The ticket barcode list is invalid.', 1;

    DECLARE @Input table (Ordinal int NOT NULL, TicketNumber nvarchar(max) NOT NULL);
    INSERT @Input (Ordinal, TicketNumber)
    SELECT CONVERT(int, [key]), LTRIM(RTRIM(CONVERT(nvarchar(max), [value])))
    FROM OPENJSON(@TicketNumbersJson) WHERE [type] = 1;

    IF (SELECT COUNT(*) FROM @Input) NOT BETWEEN 1 AND 100 THROW 50020, 'Enter between 1 and 100 ticket barcodes.', 1;
    IF EXISTS (SELECT 1 FROM @Input WHERE LEN(TicketNumber) < 6 OR LEN(TicketNumber) > 32 OR TicketNumber LIKE '%[^0-9A-Za-z-]%')
        THROW 50020, 'Every ticket barcode must contain 6 to 32 letters, numbers, or hyphens.', 1;
    IF EXISTS (SELECT TicketNumber FROM @Input GROUP BY TicketNumber HAVING COUNT(*) > 1)
        THROW 50020, 'The ticket barcode list contains a duplicate.', 1;
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

    DECLARE @BatchId uniqueidentifier = NEWID();
    DECLARE @Conflict varchar(32);
    DECLARE @Message nvarchar(2048);
    DECLARE @Created table (Id bigint PRIMARY KEY, TicketNumber varchar(32) NOT NULL);
    BEGIN TRANSACTION;

    SELECT TOP (1) @Conflict = input.TicketNumber
    FROM @Input input
    WHERE EXISTS (SELECT 1 FROM dbo.TicketVoucherIssue WITH (UPDLOCK, HOLDLOCK) WHERE TicketNumber = input.TicketNumber)
       OR EXISTS (SELECT 1 FROM dbo.Voucher WITH (UPDLOCK, HOLDLOCK) WHERE Code = input.TicketNumber)
    ORDER BY input.Ordinal;

    IF @Conflict IS NOT NULL
    BEGIN
        ROLLBACK TRANSACTION;
        SET @Message = CONCAT('Voucher benefits have already been attached to ticket ', @Conflict, '. Nothing was issued.');
        THROW 50024, @Message, 1;
    END;

    INSERT dbo.TicketVoucherIssue (BatchId, TicketPackageId, TicketNumber, IssuedBy)
    OUTPUT inserted.Id, inserted.TicketNumber INTO @Created(Id, TicketNumber)
    SELECT @BatchId, @TicketPackageId, TicketNumber, @UserName FROM @Input;

    DECLARE @CreatedVouchers table (Id bigint PRIMARY KEY);
    INSERT dbo.Voucher (TicketIssueId, PackageId, Code, DepartmentType, Amount, PairReference, IssuedBy, ExpiresUtc)
    OUTPUT inserted.Id INTO @CreatedVouchers(Id)
    SELECT issue.Id, voucherPackage.Id, issue.TicketNumber, voucherPackage.DepartmentType,
           voucherPackage.Amount, issue.TicketNumber, @UserName,
           DATEADD(day, voucherPackage.ValidDays, SYSUTCDATETIME())
    FROM @Created issue
    CROSS JOIN dbo.TicketPackageBenefit benefit
    INNER JOIN dbo.VoucherPackage voucherPackage ON voucherPackage.Id = benefit.VoucherPackageId
    WHERE benefit.TicketPackageId = @TicketPackageId AND voucherPackage.IsActive = 1;

    INSERT dbo.VoucherEvent (VoucherId, EventType, PerformedBy, Notes)
    SELECT Id, 'Issued', @UserName, 'Voucher benefit attached to an existing ticket barcode.' FROM @CreatedVouchers;
    COMMIT TRANSACTION;

    SELECT issue.Id, issue.BatchId, issue.TicketPackageId, ticketPackage.Name AS PackageName,
           ticketPackage.Description AS PackageDescription, issue.TicketNumber, issue.IssuedUtc, issue.IssuedBy
    FROM dbo.TicketVoucherIssue issue
    INNER JOIN dbo.TicketPackage ticketPackage ON ticketPackage.Id = issue.TicketPackageId
    INNER JOIN @Input input ON input.TicketNumber = issue.TicketNumber
    WHERE issue.BatchId = @BatchId
    ORDER BY input.Ordinal;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spTicketVoucherIssue_GetById @Id bigint
AS
BEGIN
    SET NOCOUNT ON;
    SELECT issue.Id, issue.BatchId, issue.TicketPackageId, ticketPackage.Name AS PackageName,
           ticketPackage.Description AS PackageDescription, issue.TicketNumber, issue.IssuedUtc, issue.IssuedBy
    FROM dbo.TicketVoucherIssue issue
    INNER JOIN dbo.TicketPackage ticketPackage ON ticketPackage.Id = issue.TicketPackageId
    WHERE issue.Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spTicketVoucherIssue_GetBatch @BatchId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SELECT issue.Id, issue.BatchId, issue.TicketPackageId, ticketPackage.Name AS PackageName,
           ticketPackage.Description AS PackageDescription, issue.TicketNumber, issue.IssuedUtc, issue.IssuedBy
    FROM dbo.TicketVoucherIssue issue
    INNER JOIN dbo.TicketPackage ticketPackage ON ticketPackage.Id = issue.TicketPackageId
    WHERE issue.BatchId = @BatchId
    ORDER BY issue.Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spTicketVoucherIssue_GetBatchVouchers @BatchId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SELECT details.*
    FROM dbo.vVoucherDetails details
    INNER JOIN dbo.TicketVoucherIssue issue ON issue.Id = details.TicketIssueId
    WHERE issue.BatchId = @BatchId
    ORDER BY issue.Id, details.DepartmentType;
END;
GO

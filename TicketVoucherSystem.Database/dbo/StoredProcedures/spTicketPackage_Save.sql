CREATE PROCEDURE dbo.spTicketPackage_Save
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

    IF NULLIF(@Name, N'') IS NULL
        THROW 50040, 'Package name is required.', 1;
    IF @RetailAmount IS NULL AND @FoodAndBeverageAmount IS NULL
        THROW 50041, 'Add a Retail amount, an F&B amount, or both.', 1;
    IF COALESCE(@RetailAmount, 1) <= 0 OR COALESCE(@FoodAndBeverageAmount, 1) <= 0 OR @ValidDays NOT BETWEEN 1 AND 3650
        THROW 50042, 'Voucher values must be positive and validity must be between 1 and 3650 days.', 1;
    DECLARE @TicketPackageId int = @Id;
    DECLARE @RetailPackageId int;
    DECLARE @FoodPackageId int;
    DECLARE @RetailName nvarchar(80) = CONCAT(@Name, N' | Retail');
    DECLARE @FoodName nvarchar(80) = CONCAT(@Name, N' | F&B');

    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM dbo.TicketPackage WITH (UPDLOCK, HOLDLOCK) WHERE Name = @Name AND Id <> @Id)
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50043, 'Another ticket package already uses this name.', 1;
    END;

    IF @TicketPackageId = 0
    BEGIN
        INSERT dbo.TicketPackage (Name, Description, CreatedBy)
        VALUES (@Name, @Description, @UserName);
        SET @TicketPackageId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.TicketPackage SET Name = @Name, Description = @Description WHERE Id = @TicketPackageId;
        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50044, 'Ticket package not found.', 1;
        END;
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
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50045, 'The generated Retail benefit name is already in use.', 1;
        END;

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
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50046, 'The generated F&B benefit name is already in use.', 1;
        END;

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

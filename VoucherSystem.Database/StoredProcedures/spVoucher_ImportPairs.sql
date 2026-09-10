CREATE PROCEDURE dbo.spVoucher_ImportPairs
    @CodesJson nvarchar(max),
    @RetailPackageId int,
    @FoodPackageId int,
    @UserName nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Codes table (Code varchar(10) PRIMARY KEY);

    INSERT @Codes (Code)
    SELECT DISTINCT CONVERT(varchar(10), [value])
    FROM OPENJSON(@CodesJson)
    WHERE [type] = 1;

    IF NOT EXISTS (SELECT 1 FROM @Codes)
        THROW 50030, 'No barcodes were supplied.', 1;

    IF EXISTS (SELECT 1 FROM @Codes WHERE Code LIKE '%[^0-9]%' OR LEN(Code) <> 10)
        THROW 50031, 'Every barcode must contain exactly 10 digits.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Voucher v
        INNER JOIN @Codes c ON c.Code = v.Code
        WHERE v.DepartmentType IN ('Retail', 'FoodAndBeverage')
    )
        THROW 50032, 'One or more barcodes already exist.', 1;

    DECLARE @RetailAmount decimal(12,2), @RetailDays int,
            @FoodAmount decimal(12,2), @FoodDays int;

    SELECT @RetailAmount = Amount, @RetailDays = ValidDays
    FROM dbo.VoucherPackage
    WHERE Id = @RetailPackageId AND DepartmentType = 'Retail' AND IsActive = 1;

    SELECT @FoodAmount = Amount, @FoodDays = ValidDays
    FROM dbo.VoucherPackage
    WHERE Id = @FoodPackageId AND DepartmentType = 'FoodAndBeverage' AND IsActive = 1;

    IF @RetailAmount IS NULL OR @FoodAmount IS NULL
        THROW 50033, 'Choose one active retail package and one active F&B package.', 1;

    DECLARE @Created table (Id bigint PRIMARY KEY, EventType varchar(30));

    BEGIN TRANSACTION;

    INSERT dbo.Voucher
        (PackageId, Code, DepartmentType, Amount, PairReference, IssuedBy, ExpiresUtc)
    OUTPUT inserted.Id, 'Imported' INTO @Created(Id, EventType)
    SELECT @RetailPackageId, Code, 'Retail', @RetailAmount, Code, @UserName,
           DATEADD(day, @RetailDays, SYSUTCDATETIME())
    FROM @Codes;

    INSERT dbo.Voucher
        (PackageId, Code, DepartmentType, Amount, PairReference, IssuedBy, ExpiresUtc)
    OUTPUT inserted.Id, 'Imported' INTO @Created(Id, EventType)
    SELECT @FoodPackageId, Code, 'FoodAndBeverage', @FoodAmount, Code, @UserName,
           DATEADD(day, @FoodDays, SYSUTCDATETIME())
    FROM @Codes;

    INSERT dbo.VoucherEvent (VoucherId, EventType, PerformedBy, Notes)
    SELECT Id, EventType, @UserName, 'Voucher created by paired barcode import.'
    FROM @Created;

    COMMIT TRANSACTION;

    SELECT COUNT(*) / 2 AS PairCount, COUNT(*) AS VoucherCount
    FROM @Created;
END;

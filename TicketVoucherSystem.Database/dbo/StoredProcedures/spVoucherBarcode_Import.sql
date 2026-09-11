CREATE PROCEDURE dbo.spVoucherBarcode_Import
    @CodesJson nvarchar(max),
    @UserName nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Codes table (Code varchar(10) PRIMARY KEY);
    INSERT @Codes (Code)
    SELECT DISTINCT CONVERT(varchar(10), [value]) FROM OPENJSON(@CodesJson) WHERE [type] = 1;

    IF NOT EXISTS (SELECT 1 FROM @Codes)
        THROW 50030, 'No barcodes were supplied.', 1;
    IF EXISTS (SELECT 1 FROM @Codes WHERE Code LIKE '%[^0-9]%' OR LEN(Code) <> 10)
        THROW 50031, 'Every barcode must contain exactly 10 digits.', 1;

    BEGIN TRANSACTION;

    DECLARE @NewCodes table (Code varchar(10) PRIMARY KEY);
    INSERT @NewCodes (Code)
    SELECT codes.Code
    FROM @Codes codes
    WHERE NOT EXISTS (SELECT 1 FROM dbo.VoucherBarcode barcode WITH (UPDLOCK, HOLDLOCK) WHERE barcode.Code = codes.Code)
      AND NOT EXISTS (SELECT 1 FROM dbo.Voucher voucher WHERE voucher.Code = codes.Code);

    INSERT dbo.VoucherBarcode (Code, DepartmentType, ImportedBy)
    SELECT Code, department.DepartmentType, @UserName
    FROM @NewCodes
    CROSS JOIN (VALUES ('Retail'), ('FoodAndBeverage')) department(DepartmentType);

    COMMIT TRANSACTION;

    SELECT
        (SELECT COUNT(*) FROM @NewCodes) AS ImportedCount,
        (SELECT COUNT(*) FROM @Codes) - (SELECT COUNT(*) FROM @NewCodes) AS DuplicateCount,
        (SELECT COUNT(*) FROM dbo.VoucherBarcode WHERE Status = 'Available') AS AvailableCount;
END;

CREATE PROCEDURE dbo.spVoucher_IssueBatch
    @PackageId int,
    @Quantity int,
    @UserName nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Quantity NOT BETWEEN 1 AND 100
        THROW 50020, 'Quantity must be between 1 and 100.', 1;

    DECLARE @DepartmentType varchar(30), @Amount decimal(12,2), @ValidDays int;
    SELECT @DepartmentType = DepartmentType, @Amount = Amount, @ValidDays = ValidDays
    FROM dbo.VoucherPackage
    WHERE Id = @PackageId AND IsActive = 1;

    IF @DepartmentType IS NULL
        THROW 50021, 'The selected voucher package is not available.', 1;

    DECLARE @Selected table (BarcodeId bigint PRIMARY KEY, Code varchar(32) NOT NULL);
    DECLARE @Created table (Id bigint PRIMARY KEY, BarcodeId bigint NOT NULL);

    BEGIN TRANSACTION;

    INSERT @Selected (BarcodeId, Code)
    SELECT TOP (@Quantity) Id, Code
    FROM dbo.VoucherBarcode WITH (UPDLOCK, READPAST, ROWLOCK)
    WHERE DepartmentType = @DepartmentType AND Status = 'Available'
    ORDER BY ImportedUtc, Id;

    IF (SELECT COUNT(*) FROM @Selected) <> @Quantity
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50022, 'There are not enough available barcodes for this voucher type. Import barcode stock first.', 1;
    END;

    INSERT dbo.Voucher (PackageId, BarcodeId, Code, DepartmentType, Amount, IssuedBy, ExpiresUtc)
    OUTPUT inserted.Id, inserted.BarcodeId INTO @Created(Id, BarcodeId)
    SELECT @PackageId, BarcodeId, Code, @DepartmentType, @Amount, @UserName,
           DATEADD(day, @ValidDays, SYSUTCDATETIME())
    FROM @Selected;

    UPDATE barcode
    SET Status = 'Assigned', AssignedUtc = SYSUTCDATETIME(), AssignedBy = @UserName
    FROM dbo.VoucherBarcode barcode
    INNER JOIN @Created created ON created.BarcodeId = barcode.Id;

    INSERT dbo.VoucherEvent (VoucherId, EventType, PerformedBy, Notes)
    SELECT Id, 'Issued', @UserName, 'Voucher allocated from existing barcode inventory.'
    FROM @Created;

    COMMIT TRANSACTION;

    SELECT details.*
    FROM dbo.vVoucherDetails details
    INNER JOIN @Created created ON created.Id = details.Id
    ORDER BY details.Id;
END;

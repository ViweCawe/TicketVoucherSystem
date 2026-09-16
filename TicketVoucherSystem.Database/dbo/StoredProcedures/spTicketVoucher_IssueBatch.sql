CREATE PROCEDURE dbo.spTicketVoucher_IssueBatch
    @TicketNumbersJson nvarchar(max),
    @TicketPackageId int,
    @UserName nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF ISJSON(@TicketNumbersJson) <> 1
        THROW 50020, 'The ticket barcode list is invalid.', 1;

    DECLARE @Input table (Ordinal int NOT NULL, TicketNumber nvarchar(max) NOT NULL);
    INSERT @Input (Ordinal, TicketNumber)
    SELECT CONVERT(int, [key]), LTRIM(RTRIM(CONVERT(nvarchar(max), [value])))
    FROM OPENJSON(@TicketNumbersJson)
    WHERE [type] = 1;

    IF (SELECT COUNT(*) FROM @Input) NOT BETWEEN 1 AND 100
        THROW 50020, 'Enter between 1 and 100 ticket barcodes.', 1;
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

CREATE PROCEDURE dbo.spTicketVoucher_Issue
    @TicketNumber varchar(32),
    @TicketPackageId int,
    @UserName nvarchar(256)
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
        SELECT 1
        FROM dbo.TicketPackageBenefit benefit
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
        GROUP BY voucherPackage.DepartmentType
        HAVING COUNT(*) > 1
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
           voucherPackage.Amount, @TicketNumber, @UserName,
           DATEADD(day, voucherPackage.ValidDays, SYSUTCDATETIME())
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

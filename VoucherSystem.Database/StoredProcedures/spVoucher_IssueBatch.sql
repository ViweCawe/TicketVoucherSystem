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

    DECLARE @DepartmentType varchar(30),
            @Amount decimal(12,2),
            @ValidDays int;

    SELECT
        @DepartmentType = DepartmentType,
        @Amount = Amount,
        @ValidDays = ValidDays
    FROM dbo.VoucherPackage
    WHERE Id = @PackageId AND IsActive = 1;

    IF @DepartmentType IS NULL
        THROW 50021, 'The selected voucher package is not available.', 1;

    DECLARE @Created table (Id bigint PRIMARY KEY);
    DECLARE @Number int = 0;

    BEGIN TRANSACTION;

    WHILE @Number < @Quantity
    BEGIN
        DECLARE @Code varchar(32) = CONCAT('TV-', LEFT(REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), 20));

        INSERT dbo.Voucher
            (PackageId, Code, DepartmentType, Amount, IssuedBy, ExpiresUtc)
        OUTPUT inserted.Id INTO @Created(Id)
        VALUES
            (@PackageId, @Code, @DepartmentType, @Amount, @UserName, DATEADD(day, @ValidDays, SYSUTCDATETIME()));

        INSERT dbo.VoucherEvent (VoucherId, EventType, PerformedBy, Notes)
        VALUES (SCOPE_IDENTITY(), 'Issued', @UserName, 'Voucher issued from the operations page.');

        SET @Number += 1;
    END;

    COMMIT TRANSACTION;

    SELECT details.*
    FROM dbo.vVoucherDetails details
    INNER JOIN @Created created ON created.Id = details.Id
    ORDER BY details.Id;
END;

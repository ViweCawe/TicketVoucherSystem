CREATE VIEW dbo.vVoucherDetails
AS
    SELECT
        v.Id,
        v.PackageId,
        p.Name AS PackageName,
        v.Code,
        v.DepartmentType,
        v.Amount,
        CASE
            WHEN v.Status = 'Issued' AND v.ExpiresUtc <= SYSUTCDATETIME() THEN 'Expired'
            ELSE v.Status
        END AS Status,
        v.PairReference,
        v.IssuedUtc,
        v.IssuedBy,
        v.ExpiresUtc,
        v.RedeemedUtc,
        v.RedeemedAt,
        v.RedeemedBy
    FROM dbo.Voucher v
    INNER JOIN dbo.VoucherPackage p ON p.Id = v.PackageId;

using TicketVoucherSystemApp.Vouchers.Db;
using TicketVoucherSystemApp.Vouchers.Models;

namespace TicketVoucherSystemApp.Vouchers.Data;

public sealed class VoucherPackageData(IDataAccess db) : IVoucherPackageData
{
    public Task<IReadOnlyList<VoucherPackage>> GetAllAsync(CancellationToken cancellationToken = default) =>
        db.QueryAsync<VoucherPackage>("dbo.spVoucherPackage_GetAll", cancellationToken: cancellationToken);

    public Task<IReadOnlyList<VoucherPackage>> GetActiveAsync(
        string? departmentType = null,
        CancellationToken cancellationToken = default) =>
        db.QueryAsync<VoucherPackage>(
            "dbo.spVoucherPackage_GetActive",
            new { DepartmentType = departmentType },
            cancellationToken);

    public async Task SaveAsync(
        VoucherPackage package,
        string userName,
        CancellationToken cancellationToken = default)
    {
        await db.ExecuteAsync(
            "dbo.spVoucherPackage_Save",
            new
            {
                package.Id,
                package.Name,
                package.Description,
                package.DepartmentType,
                package.Amount,
                package.ValidDays,
                UserName = userName
            },
            cancellationToken);
    }

    public async Task SetActiveAsync(
        int id,
        bool isActive,
        string userName,
        CancellationToken cancellationToken = default)
    {
        await db.ExecuteAsync(
            "dbo.spVoucherPackage_SetActive",
            new { Id = id, IsActive = isActive, UserName = userName },
            cancellationToken);
    }
}

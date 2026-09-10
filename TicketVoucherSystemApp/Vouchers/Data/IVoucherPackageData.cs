using TicketVoucherSystemApp.Vouchers.Models;

namespace TicketVoucherSystemApp.Vouchers.Data;

public interface IVoucherPackageData
{
    Task<IReadOnlyList<VoucherPackage>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VoucherPackage>> GetActiveAsync(string? departmentType = null, CancellationToken cancellationToken = default);
    Task SaveAsync(VoucherPackage package, string userName, CancellationToken cancellationToken = default);
    Task SetActiveAsync(int id, bool isActive, string userName, CancellationToken cancellationToken = default);
}

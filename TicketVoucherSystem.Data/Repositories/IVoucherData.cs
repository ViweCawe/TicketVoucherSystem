using TicketVoucherSystem.Data.Models;

namespace TicketVoucherSystem.Data.Repositories;

public interface IVoucherData
{
    Task<IReadOnlyList<Voucher>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Voucher>> SearchAsync(string? search, string? status, CancellationToken cancellationToken = default);
    Task<Voucher?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VoucherEvent>> GetEventsAsync(long voucherId, CancellationToken cancellationToken = default);
    Task<VoucherDashboard> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Voucher>> IssueAsync(int packageId, int quantity, string userName, CancellationToken cancellationToken = default);
    Task<Voucher?> RedeemAsync(string code, string departmentType, string location, string userName, CancellationToken cancellationToken = default);
    Task CancelAsync(long id, string reason, string userName, CancellationToken cancellationToken = default);
    Task<VoucherImportResult> ImportPairsAsync(IReadOnlyList<string> codes, int retailPackageId, int foodPackageId, string userName, CancellationToken cancellationToken = default);
}

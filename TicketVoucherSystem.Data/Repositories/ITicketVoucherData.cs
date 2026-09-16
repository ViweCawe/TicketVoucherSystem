using TicketVoucherSystem.Data.Models;

namespace TicketVoucherSystem.Data.Repositories;

public interface ITicketVoucherData
{
    Task<IReadOnlyList<TicketPackage>> GetActivePackagesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TicketPackage>> GetAllPackagesAsync(CancellationToken cancellationToken = default);
    Task SavePackageAsync(TicketPackage package, string userName, CancellationToken cancellationToken = default);
    Task SetPackageActiveAsync(int id, bool isActive, string userName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TicketVoucherIssue>> IssueBatchAsync(IReadOnlyList<string> ticketNumbers, int ticketPackageId, string userName, CancellationToken cancellationToken = default);
    Task<TicketVoucherIssue?> GetIssueAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Voucher>> GetVouchersAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TicketVoucherIssue>> GetBatchAsync(Guid batchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Voucher>> GetBatchVouchersAsync(Guid batchId, CancellationToken cancellationToken = default);
}

using TicketVoucherSystem.Data.Models;

namespace TicketVoucherSystem.Data.Repositories;

public interface ITicketVoucherData
{
    Task<IReadOnlyList<TicketPackage>> GetActivePackagesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Voucher>> IssueAsync(string ticketNumber, int ticketPackageId, string userName, CancellationToken cancellationToken = default);
    Task<TicketVoucherIssue?> GetIssueAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Voucher>> GetVouchersAsync(long id, CancellationToken cancellationToken = default);
}

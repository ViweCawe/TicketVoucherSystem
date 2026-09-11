using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Sql;

namespace TicketVoucherSystem.Data.Repositories;

public sealed class TicketVoucherData(IDataAccess db) : ITicketVoucherData
{
    public Task<IReadOnlyList<TicketPackage>> GetActivePackagesAsync(CancellationToken cancellationToken = default) =>
        db.QueryAsync<TicketPackage>("dbo.spTicketPackage_GetActive", cancellationToken: cancellationToken);

    public Task<IReadOnlyList<Voucher>> IssueAsync(
        string ticketNumber,
        int ticketPackageId,
        string userName,
        CancellationToken cancellationToken = default) =>
        db.QueryAsync<Voucher>(
            "dbo.spTicketVoucher_Issue",
            new { TicketNumber = ticketNumber.Trim(), TicketPackageId = ticketPackageId, UserName = userName },
            cancellationToken);

    public Task<TicketVoucherIssue?> GetIssueAsync(long id, CancellationToken cancellationToken = default) =>
        db.QuerySingleOrDefaultAsync<TicketVoucherIssue>("dbo.spTicketVoucherIssue_GetById", new { Id = id }, cancellationToken);

    public Task<IReadOnlyList<Voucher>> GetVouchersAsync(long id, CancellationToken cancellationToken = default) =>
        db.QueryAsync<Voucher>("dbo.spTicketVoucherIssue_GetVouchers", new { Id = id }, cancellationToken);
}

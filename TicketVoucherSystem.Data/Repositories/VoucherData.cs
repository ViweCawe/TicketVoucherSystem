using TicketVoucherSystem.Data.Sql;
using TicketVoucherSystem.Data.Models;

namespace TicketVoucherSystem.Data.Repositories;

public sealed class VoucherData(IDataAccess db) : IVoucherData
{
    public Task<IReadOnlyList<Voucher>> GetRecentAsync(
        int count = 10,
        CancellationToken cancellationToken = default) =>
        db.QueryAsync<Voucher>("dbo.spVoucher_GetRecent", new { Count = count }, cancellationToken);

    public Task<IReadOnlyList<Voucher>> SearchAsync(
        string? search,
        string? status,
        CancellationToken cancellationToken = default) =>
        db.QueryAsync<Voucher>("dbo.spVoucher_Search", new { Search = search, Status = status }, cancellationToken);

    public Task<Voucher?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        db.QuerySingleOrDefaultAsync<Voucher>("dbo.spVoucher_GetById", new { Id = id }, cancellationToken);

    public Task<IReadOnlyList<VoucherEvent>> GetEventsAsync(
        long voucherId,
        CancellationToken cancellationToken = default) =>
        db.QueryAsync<VoucherEvent>("dbo.spVoucherEvent_GetByVoucherId", new { VoucherId = voucherId }, cancellationToken);

    public async Task<VoucherDashboard> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        await db.QuerySingleOrDefaultAsync<VoucherDashboard>(
            "dbo.spVoucher_GetDashboard",
            cancellationToken: cancellationToken) ?? new VoucherDashboard();

    public Task<Voucher?> RedeemAsync(
        string code,
        string departmentType,
        int outletId,
        string userName,
        CancellationToken cancellationToken = default) =>
        db.QuerySingleOrDefaultAsync<Voucher>(
            "dbo.spVoucher_Redeem",
            new { Code = code.Trim(), DepartmentType = departmentType, OutletId = outletId, UserName = userName },
            cancellationToken);

    public async Task CancelAsync(
        long id,
        string reason,
        string userName,
        CancellationToken cancellationToken = default)
    {
        await db.ExecuteAsync(
            "dbo.spVoucher_Cancel",
            new { Id = id, Reason = reason.Trim(), UserName = userName },
            cancellationToken);
    }

}

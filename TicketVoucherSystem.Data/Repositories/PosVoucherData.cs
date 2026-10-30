using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Sql;
namespace TicketVoucherSystem.Data.Repositories;

public sealed class PosVoucherData(IDataAccess db) : IPosVoucherData
{
    public Task<Voucher?> LookupAsync(string code, string departmentType, int outletId,
        CancellationToken cancellationToken = default) => db.QuerySingleOrDefaultAsync<Voucher>(
            "dbo.spPosVoucher_Lookup", new { Code = code, DepartmentType = departmentType, OutletId = outletId }, cancellationToken);

    public Task<PosRedemption?> RedeemAsync(PosRedemptionCommand command,
        CancellationToken cancellationToken = default) => db.QuerySingleOrDefaultAsync<PosRedemption>(
            "dbo.spPosVoucher_Redeem", command, cancellationToken);
}

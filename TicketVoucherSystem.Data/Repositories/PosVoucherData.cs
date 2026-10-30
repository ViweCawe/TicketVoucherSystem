using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Sql;
namespace TicketVoucherSystem.Data.Repositories;
public sealed class PosVoucherData(IDataAccess db) : IPosVoucherData
{
    public Task<Voucher?> LookupAsync(string code, string departmentType, CancellationToken cancellationToken = default) =>
        db.QuerySingleOrDefaultAsync<Voucher>("dbo.spPosVoucher_Lookup", new { Code = code, DepartmentType = departmentType }, cancellationToken);
    public Task<Voucher?> RedeemAsync(string code, string departmentType, CancellationToken cancellationToken = default) =>
        db.QuerySingleOrDefaultAsync<Voucher>("dbo.spPosVoucher_Redeem", new { Code = code, DepartmentType = departmentType }, cancellationToken);
}

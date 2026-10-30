using TicketVoucherSystem.Data.Models;
namespace TicketVoucherSystem.Data.Repositories;
public interface IPosVoucherData
{
    Task<Voucher?> LookupAsync(string code, string departmentType, CancellationToken cancellationToken = default);
    Task<Voucher?> RedeemAsync(string code, string departmentType, CancellationToken cancellationToken = default);
}

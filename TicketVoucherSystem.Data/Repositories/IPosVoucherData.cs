using TicketVoucherSystem.Data.Models;
namespace TicketVoucherSystem.Data.Repositories;

public interface IPosVoucherData
{
    Task<Voucher?> LookupAsync(string code, string departmentType, int outletId, CancellationToken cancellationToken = default);
    Task<PosRedemption?> RedeemAsync(PosRedemptionCommand command, CancellationToken cancellationToken = default);
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketVoucherSystem.Data.Exceptions;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;
using VoucherSystemApp.API.Models;
namespace VoucherSystemApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/pos/v1/vouchers")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class VouchersController(IPosVoucherData data) : ControllerBase
{
    [HttpPost("lookup")]
    public async Task<IActionResult> Lookup(VoucherRequest request, CancellationToken cancellationToken)
    {
        var voucher = await data.LookupAsync(request.VoucherCode, request.DepartmentType, cancellationToken);
        return voucher is null ? Error(404, "voucher_not_found", "Voucher not found for this department.") : Ok(ToResponse(voucher));
    }

    [HttpPost("redeem")]
    public async Task<IActionResult> Redeem(VoucherRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var voucher = await data.RedeemAsync(request.VoucherCode, request.DepartmentType, cancellationToken);
            return voucher is null ? Error(500, "redemption_failed", "No voucher was returned.") : Ok(ToResponse(voucher));
        }
        catch (VoucherOperationException ex)
        {
            return ex.ErrorCode switch
            {
                50001 => Error(404, "voucher_not_found", "Voucher not found for this department."),
                50002 => Error(409, "already_redeemed", "Voucher has already been redeemed."),
                50003 => Error(409, "voucher_cancelled", "Voucher was cancelled."),
                50004 => Error(410, "voucher_expired", "Voucher has expired."),
                _ => Error(409, "redemption_failed", "Voucher cannot be redeemed.")
            };
        }
    }
    private static object ToResponse(Voucher v)
    {
        var status = v.Status == "Issued" && v.ExpiresUtc <= DateTime.UtcNow ? "Expired" : v.Status;
        return new { voucherId = v.Id, voucherCode = v.Code, v.DepartmentType, voucherAmount = v.Amount,
            currency = "ZAR", status, isValid = status == "Issued", isRedeemed = status == "Redeemed",
            isExpired = status == "Expired", expiresUtc = DateTime.SpecifyKind(v.ExpiresUtc, DateTimeKind.Utc),
            redeemedUtc = v.RedeemedUtc is DateTime date ? (DateTime?)DateTime.SpecifyKind(date, DateTimeKind.Utc) : null };
    }
    private ObjectResult Error(int status, string code, string message) => StatusCode(status, new { code, message });
}

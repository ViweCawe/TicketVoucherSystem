using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TicketVoucherSystem.Data.Exceptions;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;
using VoucherSystemApp.API.Models;

namespace VoucherSystemApp.API.Controllers;

[ApiController]
[Route("api/pos/v1/vouchers")]
[Authorize(Policy = "Pos")]
[EnableRateLimiting("pos")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class VouchersController(IPosVoucherData data) : ControllerBase
{
    [HttpPost("lookup")]
    public async Task<IActionResult> Lookup(LookupRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var voucher = await data.LookupAsync(request.VoucherCode, Claim("department"), OutletId(), cancellationToken);
            if (voucher is null) return Error(404, "voucher_not_found", "No voucher was issued for this barcode and department.");
            var status = voucher.Status == "Issued" && voucher.ExpiresUtc <= DateTime.UtcNow ? "Expired" : voucher.Status;
            var canRedeem = status == "Issued" && request.EligibleTotal >= voucher.Amount;
            return Ok(new { voucherId = voucher.Id, voucherCode = voucher.Code, voucher.PackageName,
                voucher.DepartmentType, voucherAmount = voucher.Amount, currency = "ZAR", status,
                expiresUtc = DateTime.SpecifyKind(voucher.ExpiresUtc, DateTimeKind.Utc),
                canRedeem, reason = status != "Issued" ? "voucher_" + status.ToLowerInvariant()
                    : !canRedeem ? "insufficient_eligible_total" : null,
                request.SaleTotal, request.EligibleTotal,
                discountAmount = canRedeem ? voucher.Amount : 0,
                amountDue = request.SaleTotal - (canRedeem ? voucher.Amount : 0) });
        }
        catch (VoucherOperationException ex) { return OperationError(ex); }
    }

    [HttpPost("redeem")]
    public async Task<IActionResult> Redeem(RedeemRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Outlet, till and department come from the authenticated token, never from the request body.
            var receipt = await data.RedeemAsync(new PosRedemptionCommand(Claim("sub"), Claim("till_id"),
                OutletId(), Claim("department"), request.IdempotencyKey, request.TransactionId,
                request.OperatorId, request.VoucherCode, request.SaleTotal, request.EligibleTotal, request.Currency), cancellationToken);
            if (receipt is null) return Error(500, "redemption_failed", "No redemption receipt was returned.");
            receipt.RedeemedUtc = DateTime.SpecifyKind(receipt.RedeemedUtc, DateTimeKind.Utc);
            return Ok(receipt);
        }
        catch (VoucherOperationException ex) { return OperationError(ex); }
    }

    private string Claim(string name) => User.FindFirst(name)!.Value;
    private int OutletId() => int.Parse(Claim("outlet_id"), CultureInfo.InvariantCulture);
    private ObjectResult Error(int status, string code, string message) => StatusCode(status,
        new { code, message, traceId = HttpContext.TraceIdentifier });
    private ObjectResult OperationError(VoucherOperationException ex) => ex.ErrorCode switch
    {
        50001 => Error(404, "voucher_not_found", "No voucher was issued for this barcode and department."),
        50002 => Error(409, "already_redeemed", "This voucher has already been redeemed."),
        50003 => Error(409, "voucher_cancelled", "This voucher was cancelled."),
        50004 => Error(410, "voucher_expired", "This voucher has expired."),
        50006 => Error(403, "invalid_outlet", "This outlet is not active."),
        50101 => Error(409, "idempotency_conflict", "This key was used with different redemption details."),
        50102 => Error(409, "transaction_already_used", "This sale already has a voucher redemption; retry its original request."),
        50103 => Error(422, "insufficient_eligible_total", "Eligible purchases must cover the full voucher amount."),
        50104 => Error(400, "invalid_request", "Check the sale amounts, currency and identifiers."),
        50105 => Error(503, "redemption_busy", "Retry using the same idempotency key and request."),
        _ => Error(500, "redemption_failed", "The voucher could not be redeemed; retry the original request.")
    };
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using TicketVoucherSystemApp.Vouchers.Data;
using TicketVoucherSystemApp.Vouchers.Models;
using TicketVoucherSystemApp.Services;

namespace TicketVoucherSystemApp.Pages.Admin.Vouchers;

public sealed class DetailsModel(IVoucherData voucherData, IVoucherBarcodeExporter barcodeExporter) : PageModel
{
    [BindProperty]
    public string CancellationReason { get; set; } = string.Empty;

    public Voucher Voucher { get; private set; } = new();
    public IReadOnlyList<VoucherEvent> Events { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var voucher = await voucherData.GetByIdAsync(id, cancellationToken);
        if (voucher is null)
        {
            return NotFound();
        }

        Voucher = voucher;
        Events = await voucherData.GetEventsAsync(id, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnGetBarcodeAsync(long id, CancellationToken cancellationToken)
    {
        var voucher = await voucherData.GetByIdAsync(id, cancellationToken);
        return voucher is null
            ? NotFound()
            : File(barcodeExporter.CreateSvg(voucher), "image/svg+xml", $"voucher-{voucher.Code}.svg");
    }

    public async Task<IActionResult> OnPostCancelAsync(long id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(CancellationReason))
        {
            ModelState.AddModelError(nameof(CancellationReason), "Enter a reason for cancelling this voucher.");
            return await OnGetAsync(id, cancellationToken);
        }

        try
        {
            await voucherData.CancelAsync(
                id,
                CancellationReason,
                User.Identity?.Name ?? "System",
                cancellationToken);
        }
        catch (SqlException exception) when (exception.Number == 50040)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return await OnGetAsync(id, cancellationToken);
        }

        return RedirectToPage(new { id });
    }
}

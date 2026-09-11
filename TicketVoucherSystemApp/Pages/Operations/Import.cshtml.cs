using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketVoucherSystem.Data.Exceptions;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;
using TicketVoucherSystem.Data.Validation;

namespace TicketVoucherSystemApp.Pages.Operations;

public sealed class ImportModel(IVoucherBarcodeData barcodes) : PageModel
{
    [BindProperty]
    public string BarcodeList { get; set; } = string.Empty;

    public BarcodeImportResult? Result { get; private set; }
    public BarcodeStock Stock { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Stock = await barcodes.GetStockAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            var codes = VoucherCodeList.Parse(BarcodeList);
            Result = await barcodes.ImportAsync(
                codes,
                User.Identity?.Name ?? "System",
                cancellationToken);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(nameof(BarcodeList), exception.Message);
        }
        catch (VoucherOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }

        Stock = await barcodes.GetStockAsync(cancellationToken);
        return Page();
    }
}

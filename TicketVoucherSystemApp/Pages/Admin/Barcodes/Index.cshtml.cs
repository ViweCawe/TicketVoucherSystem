using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;

namespace TicketVoucherSystemApp.Pages.Admin.Barcodes;

public sealed class IndexModel(IVoucherBarcodeData barcodes) : PageModel
{
    public string? Search { get; private set; }
    public string? Status { get; private set; }
    public BarcodeStock Stock { get; private set; } = new();
    public IReadOnlyList<VoucherBarcode> Barcodes { get; private set; } = [];

    public async Task OnGetAsync(string? search, string? status, CancellationToken cancellationToken)
    {
        Search = search;
        Status = status;
        var stockTask = barcodes.GetStockAsync(cancellationToken);
        var searchTask = barcodes.SearchAsync(search, status, cancellationToken);
        await Task.WhenAll(stockTask, searchTask);
        Stock = await stockTask;
        Barcodes = await searchTask;
    }
}

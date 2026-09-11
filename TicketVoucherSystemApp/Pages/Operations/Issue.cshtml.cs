using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketVoucherSystem.Data.Exceptions;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;
using TicketVoucherSystemApp.Services;

namespace TicketVoucherSystemApp.Pages.Operations;

public sealed class IssueModel(
    IVoucherData vouchers,
    IVoucherPackageData packages,
    IVoucherBarcodeData barcodes,
    IVoucherBarcodeExporter barcodeExporter) : PageModel
{
    [BindProperty]
    public int PackageId { get; set; }

    [BindProperty]
    public int Quantity { get; set; } = 1;

    public IReadOnlyList<VoucherPackage> Packages { get; private set; } = [];
    public IReadOnlyList<Voucher> RecentVouchers { get; private set; } = [];
    public IReadOnlyList<Voucher> IssuedVouchers { get; private set; } = [];
    public BarcodeStock Stock { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (PackageId <= 0 || Quantity is < 1 or > 100)
        {
            ModelState.AddModelError(string.Empty, "Choose a package and enter a quantity from 1 to 100.");
        }
        else
        {
            try
            {
                IssuedVouchers = await vouchers.IssueAsync(PackageId, Quantity, UserName, cancellationToken);
            }
            catch (VoucherOperationException exception)
            {
                ModelState.AddModelError(string.Empty, exception.Message);
            }
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnGetBarcodeAsync(long id, CancellationToken cancellationToken)
    {
        var voucher = await vouchers.GetByIdAsync(id, cancellationToken);
        return voucher is null || !CanExport(voucher)
            ? NotFound()
            : File(barcodeExporter.CreateSvg(voucher), "image/svg+xml", $"voucher-{voucher.Code}.svg");
    }

    public async Task<IActionResult> OnGetBatchAsync([FromQuery] long[] ids, CancellationToken cancellationToken)
    {
        var selected = new List<Voucher>();
        foreach (var id in ids.Distinct().Take(100))
        {
            var voucher = await vouchers.GetByIdAsync(id, cancellationToken);
            if (voucher is not null && CanExport(voucher))
            {
                selected.Add(voucher);
            }
        }

        return selected.Count == 0
            ? NotFound()
            : File(barcodeExporter.CreateZip(selected), "application/zip", $"vouchers-{DateTime.UtcNow:yyyyMMdd-HHmm}.zip");
    }

    private string UserName => User.Identity?.Name ?? "System";

    private bool CanExport(Voucher voucher) =>
        User.IsInRole("Admin") || string.Equals(voucher.IssuedBy, UserName, StringComparison.OrdinalIgnoreCase);

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var packagesTask = packages.GetActiveAsync(cancellationToken: cancellationToken);
        var recentTask = vouchers.GetRecentAsync(cancellationToken: cancellationToken);
        var stockTask = barcodes.GetStockAsync(cancellationToken);
        await Task.WhenAll(packagesTask, recentTask, stockTask);
        Packages = await packagesTask;
        RecentVouchers = await recentTask;
        Stock = await stockTask;
    }
}

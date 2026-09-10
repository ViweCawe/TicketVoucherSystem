using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using TicketVoucherSystemApp.Vouchers.Data;
using TicketVoucherSystemApp.Vouchers.Models;
using TicketVoucherSystemApp.Services;

namespace TicketVoucherSystemApp.Pages.Operations;

public sealed class IssueModel(
    IVoucherData voucherData,
    IVoucherPackageData packageData,
    IVoucherBarcodeExporter barcodeExporter) : PageModel
{
    [BindProperty]
    public int PackageId { get; set; }

    [BindProperty]
    public int Quantity { get; set; } = 1;

    public IReadOnlyList<VoucherPackage> Packages { get; private set; } = [];
    public IReadOnlyList<Voucher> RecentVouchers { get; private set; } = [];
    public IReadOnlyList<Voucher> IssuedVouchers { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        await LoadPageAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (PackageId <= 0 || Quantity is < 1 or > 100)
        {
            ModelState.AddModelError(string.Empty, "Choose a package and enter a quantity from 1 to 100.");
            await LoadPageAsync(cancellationToken);
            return Page();
        }

        try
        {
            IssuedVouchers = await voucherData.IssueAsync(PackageId, Quantity, UserName, cancellationToken);
        }
        catch (SqlException exception) when (exception.Number is 50020 or 50021)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }

        await LoadPageAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnGetBarcodeAsync(long id, CancellationToken cancellationToken)
    {
        var voucher = await voucherData.GetByIdAsync(id, cancellationToken);
        return voucher is null || !CanExport(voucher)
            ? NotFound()
            : File(barcodeExporter.CreateSvg(voucher), "image/svg+xml", $"voucher-{voucher.Code}.svg");
    }

    public async Task<IActionResult> OnGetBatchAsync([FromQuery] long[] ids, CancellationToken cancellationToken)
    {
        var vouchers = new List<Voucher>();
        foreach (var id in ids.Distinct().Take(100))
        {
            var voucher = await voucherData.GetByIdAsync(id, cancellationToken);
            if (voucher is not null && CanExport(voucher))
            {
                vouchers.Add(voucher);
            }
        }

        return vouchers.Count == 0
            ? NotFound()
            : File(barcodeExporter.CreateZip(vouchers), "application/zip", $"vouchers-{DateTime.UtcNow:yyyyMMdd-HHmm}.zip");
    }

    private string UserName => User.Identity?.Name ?? "System";

    private bool CanExport(Voucher voucher) =>
        User.IsInRole("Admin") || string.Equals(voucher.IssuedBy, UserName, StringComparison.OrdinalIgnoreCase);

    private async Task LoadPageAsync(CancellationToken cancellationToken)
    {
        Packages = await packageData.GetActiveAsync(cancellationToken: cancellationToken);
        RecentVouchers = await voucherData.GetRecentAsync(cancellationToken: cancellationToken);
    }
}

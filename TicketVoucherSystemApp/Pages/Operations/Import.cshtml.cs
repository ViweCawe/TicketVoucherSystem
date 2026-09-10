using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using TicketVoucherSystemApp.Vouchers.Data;
using TicketVoucherSystemApp.Vouchers.Models;
using TicketVoucherSystemApp.Vouchers.Validation;

namespace TicketVoucherSystemApp.Pages.Operations;

public sealed class ImportModel(IVoucherData voucherData, IVoucherPackageData packageData) : PageModel
{
    [BindProperty]
    public int RetailPackageId { get; set; }

    [BindProperty]
    public int FoodPackageId { get; set; }

    [BindProperty]
    public string Barcodes { get; set; } = string.Empty;

    public IReadOnlyList<VoucherPackage> RetailPackages { get; private set; } = [];
    public IReadOnlyList<VoucherPackage> FoodPackages { get; private set; } = [];
    public VoucherImportResult? Result { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        await LoadPackagesAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            var codes = VoucherCodeList.Parse(Barcodes);
            Result = await voucherData.ImportPairsAsync(
                codes,
                RetailPackageId,
                FoodPackageId,
                User.Identity?.Name ?? "System",
                cancellationToken);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(nameof(Barcodes), exception.Message);
        }
        catch (SqlException exception) when (exception.Number is >= 50030 and <= 50033)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }

        await LoadPackagesAsync(cancellationToken);
        return Page();
    }

    private async Task LoadPackagesAsync(CancellationToken cancellationToken)
    {
        RetailPackages = await packageData.GetActiveAsync(VoucherDepartments.Retail, cancellationToken);
        FoodPackages = await packageData.GetActiveAsync(VoucherDepartments.FoodAndBeverage, cancellationToken);
    }
}

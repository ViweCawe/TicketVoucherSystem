using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using TicketVoucherSystemApp.Vouchers.Data;
using TicketVoucherSystemApp.Vouchers.Models;

namespace TicketVoucherSystemApp.Pages.Operations;

public sealed class RedeemModel(IVoucherData voucherData) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Department { get; set; } = VoucherDepartments.Retail;

    [BindProperty]
    public string Code { get; set; } = string.Empty;

    [BindProperty]
    public string Location { get; set; } = string.Empty;

    public Voucher? RedeemedVoucher { get; private set; }

    public void OnGet()
    {
        if (!VoucherDepartments.All.Contains(Department))
        {
            Department = VoucherDepartments.Retail;
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        OnGet();
        if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Location))
        {
            ModelState.AddModelError(string.Empty, "Enter the voucher barcode and redemption location.");
            return Page();
        }

        try
        {
            RedeemedVoucher = await voucherData.RedeemAsync(
                Code,
                Department,
                Location,
                User.Identity?.Name ?? "System",
                cancellationToken);
        }
        catch (SqlException exception) when (exception.Number is >= 50001 and <= 50005)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }

        return Page();
    }
}

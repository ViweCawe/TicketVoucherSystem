using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketVoucherSystem.Data.Exceptions;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;

namespace TicketVoucherSystemApp.Pages.Operations;

public sealed class RedeemModel(IVoucherData vouchers, IOutletData outlets) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Department { get; set; } = VoucherDepartments.Retail;

    [BindProperty]
    public string Code { get; set; } = string.Empty;

    [BindProperty]
    public int OutletId { get; set; }

    public IReadOnlyList<Outlet> Outlets { get; private set; } = [];
    public Voucher? RedeemedVoucher { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(Code) || OutletId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Select an outlet and scan the voucher barcode.");
            return Page();
        }

        try
        {
            RedeemedVoucher = await vouchers.RedeemAsync(
                Code,
                Department,
                OutletId,
                User.Identity?.Name ?? "System",
                cancellationToken);
            Code = string.Empty;
            ModelState.Remove(nameof(Code));
        }
        catch (VoucherOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }

        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (!VoucherDepartments.All.Contains(Department))
        {
            Department = VoucherDepartments.Retail;
        }

        Outlets = await outlets.GetActiveAsync(cancellationToken);
    }
}

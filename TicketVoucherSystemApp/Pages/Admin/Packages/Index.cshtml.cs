using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketVoucherSystem.Data.Exceptions;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;

namespace TicketVoucherSystemApp.Pages.Admin.Packages;

public sealed class IndexModel(ITicketVoucherData tickets) : PageModel
{
    [BindProperty]
    public TicketPackage Package { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public IReadOnlyList<TicketPackage> Packages { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken)
    {
        if (Package.RetailAmount is null && Package.FoodAndBeverageAmount is null)
        {
            ModelState.AddModelError(string.Empty, "Add a Retail value, an F&B value, or both.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        try
        {
            await tickets.SavePackageAsync(
                Package,
                User.Identity?.Name ?? "System",
                cancellationToken);
            StatusMessage = Package.Id == 0 ? "Package created." : "Package updated.";
            return RedirectToPage();
        }
        catch (VoucherOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        try
        {
            await tickets.SetPackageActiveAsync(
                id,
                isActive,
                User.Identity?.Name ?? "System",
                cancellationToken);
            StatusMessage = isActive ? "Package activated." : "Package deactivated.";
        }
        catch (VoucherOperationException exception)
        {
            StatusMessage = exception.Message;
        }

        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken) =>
        Packages = await tickets.GetAllPackagesAsync(cancellationToken);
}

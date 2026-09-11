using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketVoucherSystem.Data.Exceptions;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;

namespace TicketVoucherSystemApp.Pages.Operations;

public sealed class IssueModel(ITicketVoucherData tickets) : PageModel
{
    [BindProperty]
    [Required, StringLength(32, MinimumLength = 6)]
    [RegularExpression("^[0-9A-Za-z-]+$", ErrorMessage = "Use only letters, numbers, and hyphens.")]
    public string TicketNumber { get; set; } = string.Empty;

    [BindProperty]
    [Range(1, int.MaxValue, ErrorMessage = "Choose a ticket package.")]
    public int TicketPackageId { get; set; }

    public IReadOnlyList<TicketPackage> Packages { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        try
        {
            var vouchers = await tickets.IssueAsync(
                TicketNumber,
                TicketPackageId,
                User.Identity?.Name ?? "System",
                cancellationToken);
            var issueId = vouchers.FirstOrDefault()?.TicketIssueId;
            if (!issueId.HasValue)
            {
                ModelState.AddModelError(string.Empty, "The ticket package did not create any voucher benefits.");
                await LoadAsync(cancellationToken);
                return Page();
            }

            return RedirectToPage("/Tickets/Print", new { id = issueId.Value });
        }
        catch (VoucherOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadAsync(cancellationToken);
            return Page();
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken) =>
        Packages = await tickets.GetActivePackagesAsync(cancellationToken);
}

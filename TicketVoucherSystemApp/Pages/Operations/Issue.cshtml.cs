using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketVoucherSystem.Data.Exceptions;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;
using TicketVoucherSystem.Data.Validation;

namespace TicketVoucherSystemApp.Pages.Operations;

public sealed class IssueModel(ITicketVoucherData tickets) : PageModel
{
    [BindProperty]
    [Required(ErrorMessage = "Scan or enter at least one ticket barcode.")]
    public string TicketNumbers { get; set; } = string.Empty;

    [BindProperty]
    [Range(1, int.MaxValue, ErrorMessage = "Choose a ticket package.")]
    public int TicketPackageId { get; set; }

    public IReadOnlyList<TicketPackage> Packages { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<string> ticketNumbers = [];
        if (ModelState.IsValid)
        {
            try
            {
                ticketNumbers = TicketNumberList.Parse(TicketNumbers);
            }
            catch (ArgumentException exception)
            {
                ModelState.AddModelError(nameof(TicketNumbers), exception.Message);
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        try
        {
            var issues = await tickets.IssueBatchAsync(
                ticketNumbers,
                TicketPackageId,
                User.Identity?.Name ?? "System",
                cancellationToken);

            var batchId = issues.FirstOrDefault()?.BatchId;
            if (!batchId.HasValue || batchId == Guid.Empty)
            {
                ModelState.AddModelError(string.Empty, "The ticket package did not create any voucher benefits.");
                await LoadAsync(cancellationToken);
                return Page();
            }

            return RedirectToPage("/Tickets/PrintBatch", new { batchId });
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

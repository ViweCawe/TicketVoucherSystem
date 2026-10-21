using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketVoucherSystem.Data.Exceptions;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;
using TicketVoucherSystemApp.Integrations.WebTickets;

namespace TicketVoucherSystemApp.Pages.Admin.WebTickets;

public sealed class IndexModel(IWebTicketsClient webTickets, ITicketVoucherData tickets) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Scan or enter a Webtickets barcode.")]
    public string Barcode { get; set; } = string.Empty;

    [BindProperty]
    public int TicketPackageId { get; set; }

    [BindProperty]
    public List<string> SelectedBarcodes { get; set; } = [];

    public WebTicketsLookup? Lookup { get; private set; }
    public IReadOnlyList<TicketPackage> Packages { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        await LoadPackagesAsync(cancellationToken);

    public async Task<IActionResult> OnPostLookupAsync(CancellationToken cancellationToken)
    {
        ModelState.Remove(nameof(TicketPackageId));
        ModelState.Remove(nameof(SelectedBarcodes));
        if (ModelState.IsValid)
            Lookup = await TryLookupAsync(cancellationToken);

        await LoadPackagesAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAssignAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(Barcode))
            Lookup = await TryLookupAsync(cancellationToken);

        if (TicketPackageId < 1)
            ModelState.AddModelError(nameof(TicketPackageId), "Choose a voucher package.");

        var selectedBarcodes = SelectedBarcodes
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (selectedBarcodes.Length == 0)
            ModelState.AddModelError(nameof(SelectedBarcodes), "Select at least one ticket.");
        else if (selectedBarcodes.Length > 100)
            ModelState.AddModelError(nameof(SelectedBarcodes), "Select no more than 100 tickets.");

        IReadOnlyList<string> verifiedBarcodes = [];
        if (Lookup is not null && selectedBarcodes.Length > 0)
        {
            var authorizedTickets = Lookup.Transaction.Tickets
                .Where(ticket => ticket.Authorized)
                .ToLookup(ticket => ticket.Barcode, StringComparer.OrdinalIgnoreCase);

            if (selectedBarcodes.Any(barcode => !authorizedTickets.Contains(barcode)))
            {
                ModelState.AddModelError(
                    nameof(SelectedBarcodes),
                    "The transaction changed or contains an unauthorized selection. Review the tickets and try again.");
            }
            else
            {
                verifiedBarcodes = selectedBarcodes
                    .Select(barcode => authorizedTickets[barcode].First().Barcode)
                    .ToArray();
            }
        }

        if (Lookup is null || !ModelState.IsValid)
        {
            await LoadPackagesAsync(cancellationToken);
            return Page();
        }

        try
        {
            var issues = await tickets.IssueBatchAsync(
                verifiedBarcodes, TicketPackageId,
                User.Identity?.Name ?? "System", cancellationToken);
            var batchId = issues.FirstOrDefault()?.BatchId;
            if (!batchId.HasValue || batchId == Guid.Empty)
            {
                ModelState.AddModelError(string.Empty, "The selected package did not create any voucher benefits.");
                await LoadPackagesAsync(cancellationToken);
                return Page();
            }

            return RedirectToPage("/Tickets/PrintBatch", new { batchId });
        }
        catch (VoucherOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadPackagesAsync(cancellationToken);
            return Page();
        }
    }

    private async Task<WebTicketsLookup?> TryLookupAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await webTickets.LookupAsync(Barcode, cancellationToken);
        }
        catch (WebTicketsLookupException exception)
        {
            ModelState.AddModelError(nameof(Barcode), exception.Message);
            return null;
        }
    }

    private async Task LoadPackagesAsync(CancellationToken cancellationToken) =>
        Packages = await tickets.GetActivePackagesAsync(cancellationToken);
}

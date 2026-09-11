using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;
using TicketVoucherSystemApp.Services;

namespace TicketVoucherSystemApp.Pages.Tickets;

public sealed class PrintModel(
    ITicketVoucherData tickets,
    IVoucherBarcodeExporter barcodes) : PageModel
{
    public TicketVoucherIssue Issue { get; private set; } = new();
    public IReadOnlyList<Voucher> Benefits { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var issue = await tickets.GetIssueAsync(id, cancellationToken);
        if (issue is null)
        {
            return NotFound();
        }

        Issue = issue;
        Benefits = await tickets.GetVouchersAsync(id, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnGetBarcodeAsync(long id, bool download, CancellationToken cancellationToken)
    {
        var issue = await tickets.GetIssueAsync(id, cancellationToken);
        if (issue is null)
        {
            return NotFound();
        }

        var content = barcodes.CreateTicketSvg(issue.TicketNumber);
        return download
            ? File(content, "image/svg+xml", $"ticket-{issue.TicketNumber}.svg")
            : File(content, "image/svg+xml");
    }
}

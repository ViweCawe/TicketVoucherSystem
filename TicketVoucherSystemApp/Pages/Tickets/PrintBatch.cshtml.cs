using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;

namespace TicketVoucherSystemApp.Pages.Tickets;

public sealed class PrintBatchModel(ITicketVoucherData tickets) : PageModel
{
    public IReadOnlyList<TicketVoucherIssue> Issues { get; private set; } = [];
    public IReadOnlyList<Voucher> Vouchers { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid batchId, CancellationToken cancellationToken)
    {
        Issues = await tickets.GetBatchAsync(batchId, cancellationToken);
        if (Issues.Count == 0)
        {
            return NotFound();
        }

        Vouchers = await tickets.GetBatchVouchersAsync(batchId, cancellationToken);
        return Page();
    }

    public IEnumerable<Voucher> BenefitsFor(long issueId) =>
        Vouchers.Where(voucher => voucher.TicketIssueId == issueId);
}

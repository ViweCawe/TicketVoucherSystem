using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketVoucherSystemApp.Vouchers.Data;
using TicketVoucherSystemApp.Vouchers.Models;

namespace TicketVoucherSystemApp.Pages.Admin.Vouchers;

public sealed class IndexModel(IVoucherData voucherData) : PageModel
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public IReadOnlyList<Voucher> Vouchers { get; private set; } = [];
    public VoucherDashboard Dashboard { get; private set; } = new();

    public async Task OnGetAsync(string? search, string? status, CancellationToken cancellationToken)
    {
        Search = search;
        Status = status;
        Vouchers = await voucherData.SearchAsync(search, status, cancellationToken);
        Dashboard = await voucherData.GetDashboardAsync(cancellationToken);
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;

namespace TicketVoucherSystemApp.Pages.Dashboard;

public sealed class IndexModel(
    IVoucherReportData reports,
    IVoucherData vouchers) : PageModel
{
    public VoucherReportSummary Summary { get; private set; } = new();
    public IReadOnlyList<VoucherTrend> Trend { get; private set; } = [];
    public IReadOnlyList<Voucher> RecentVouchers { get; private set; } = [];
    public int MaxDailyCount => Math.Max(1, Trend.Select(day => Math.Max(day.IssuedCount, day.RedeemedCount)).DefaultIfEmpty(1).Max());

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var summaryTask = reports.GetSummaryAsync(30, cancellationToken);
        var trendTask = reports.GetTrendAsync(7, cancellationToken);
        var recentTask = vouchers.GetRecentAsync(8, cancellationToken);

        await Task.WhenAll(summaryTask, trendTask, recentTask);
        Summary = await summaryTask;
        Trend = await trendTask;
        RecentVouchers = await recentTask;
    }
}

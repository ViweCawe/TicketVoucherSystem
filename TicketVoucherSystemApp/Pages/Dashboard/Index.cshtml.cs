using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;
using TicketVoucherSystemApp.Services;

namespace TicketVoucherSystemApp.Pages.Dashboard;

public sealed class IndexModel(IVoucherReportData reports, IVoucherData vouchers) : PageModel
{
    public VoucherReportSummary Summary { get; private set; } = new();
    public IReadOnlyList<VoucherTrend> Trend { get; private set; } = [];
    public IReadOnlyList<VoucherOutletReport> Outlets { get; private set; } = [];
    public IReadOnlyList<Voucher> RecentVouchers { get; private set; } = [];
    public string LinePoints => ReportChart.LinePoints(Trend);
    public string DonutStyle => ReportChart.DonutBackground(Outlets);
    public IReadOnlyList<string> Colors => ReportChart.Colors;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var summaryTask = reports.GetSummaryAsync(30, cancellationToken);
        var trendTask = reports.GetTrendAsync(7, cancellationToken);
        var outletsTask = reports.GetOutletsAsync(30, cancellationToken);
        var recentTask = vouchers.GetRecentAsync(7, cancellationToken);
        await Task.WhenAll(summaryTask, trendTask, outletsTask, recentTask);
        Summary = await summaryTask;
        Trend = await trendTask;
        Outlets = await outletsTask;
        RecentVouchers = await recentTask;
    }
}

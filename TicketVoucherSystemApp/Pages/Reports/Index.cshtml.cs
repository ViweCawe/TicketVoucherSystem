using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;
using TicketVoucherSystemApp.Services;

namespace TicketVoucherSystemApp.Pages.Reports;

public sealed class IndexModel(IVoucherReportData reports) : PageModel
{
    public int Days { get; private set; } = 30;
    public VoucherReportSummary Summary { get; private set; } = new();
    public IReadOnlyList<VoucherTrend> Trend { get; private set; } = [];
    public IReadOnlyList<VoucherOutletReport> Outlets { get; private set; } = [];
    public decimal MaxOutletValue => Math.Max(1m, Outlets.Select(outlet => outlet.RedeemedValue).DefaultIfEmpty(1).Max());
    public string LinePoints => ReportChart.LinePoints(Trend);
    public string DonutStyle => ReportChart.DonutBackground(Outlets);
    public IReadOnlyList<string> Colors => ReportChart.Colors;

    public async Task OnGetAsync(int days = 30, CancellationToken cancellationToken = default) =>
        await LoadAsync(days, cancellationToken);

    public async Task<IActionResult> OnGetCsvAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        await LoadAsync(days, cancellationToken);
        var csv = new StringBuilder("Outlet,Redeemed Count,Redeemed Value,Share Percent\r\n");
        foreach (var outlet in Outlets)
        {
            csv.AppendLine($"{Csv(outlet.OutletName)},{outlet.RedeemedCount},{outlet.RedeemedValue:0.00},{outlet.SharePercent:0.0}");
        }

        return File(new UTF8Encoding(true).GetBytes(csv.ToString()), "text/csv", $"outlet-redemptions-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private async Task LoadAsync(int days, CancellationToken cancellationToken)
    {
        Days = days is 7 or 30 or 90 ? days : 30;
        var summaryTask = reports.GetSummaryAsync(Days, cancellationToken);
        var trendTask = reports.GetTrendAsync(Days, cancellationToken);
        var outletsTask = reports.GetOutletsAsync(Days, cancellationToken);
        await Task.WhenAll(summaryTask, trendTask, outletsTask);
        Summary = await summaryTask;
        Trend = await trendTask;
        Outlets = await outletsTask;
    }

    private static string Csv(string value)
    {
        var safe = value.Length > 0 && "=+-@".Contains(value[0]) ? $"'{value}" : value;
        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }
}

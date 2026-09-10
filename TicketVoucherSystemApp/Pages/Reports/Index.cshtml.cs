using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;

namespace TicketVoucherSystemApp.Pages.Reports;

public sealed class IndexModel(IVoucherReportData reports) : PageModel
{
    public int Days { get; private set; } = 30;
    public VoucherReportSummary Summary { get; private set; } = new();
    public IReadOnlyList<VoucherTrend> Trend { get; private set; } = [];
    public IReadOnlyList<VoucherDepartmentReport> Departments { get; private set; } = [];
    public decimal MaxDailyValue => Math.Max(1, Trend.Select(day => Math.Max(day.IssuedValue, day.RedeemedValue)).DefaultIfEmpty(1).Max());

    public async Task OnGetAsync(int days = 30, CancellationToken cancellationToken = default) =>
        await LoadAsync(days, cancellationToken);

    public async Task<IActionResult> OnGetCsvAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        await LoadAsync(days, cancellationToken);
        var csv = new StringBuilder("Date,Issued Count,Redeemed Count,Issued Value,Redeemed Value\r\n");
        foreach (var day in Trend)
        {
            csv.AppendLine($"{day.ReportDate:yyyy-MM-dd},{day.IssuedCount},{day.RedeemedCount},{day.IssuedValue:0.00},{day.RedeemedValue:0.00}");
        }

        return File(
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv.ToString()),
            "text/csv",
            $"voucher-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private async Task LoadAsync(int days, CancellationToken cancellationToken)
    {
        Days = days is 7 or 30 or 90 ? days : 30;
        var summaryTask = reports.GetSummaryAsync(Days, cancellationToken);
        var trendTask = reports.GetTrendAsync(Days, cancellationToken);
        var departmentsTask = reports.GetDepartmentsAsync(Days, cancellationToken);

        await Task.WhenAll(summaryTask, trendTask, departmentsTask);
        Summary = await summaryTask;
        Trend = await trendTask;
        Departments = await departmentsTask;
    }
}

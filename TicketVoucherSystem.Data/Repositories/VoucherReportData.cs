using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Sql;

namespace TicketVoucherSystem.Data.Repositories;

public sealed class VoucherReportData(IDataAccess db) : IVoucherReportData
{
    public async Task<VoucherReportSummary> GetSummaryAsync(
        int days,
        CancellationToken cancellationToken = default) =>
        await db.QuerySingleOrDefaultAsync<VoucherReportSummary>(
            "dbo.spReport_GetExecutiveSummary",
            new { Days = NormaliseDays(days) },
            cancellationToken) ?? new VoucherReportSummary();

    public Task<IReadOnlyList<VoucherTrend>> GetTrendAsync(
        int days,
        CancellationToken cancellationToken = default) =>
        db.QueryAsync<VoucherTrend>(
            "dbo.spReport_GetVoucherTrend",
            new { Days = NormaliseDays(days) },
            cancellationToken);

    public Task<IReadOnlyList<VoucherDepartmentReport>> GetDepartmentsAsync(
        int days,
        CancellationToken cancellationToken = default) =>
        db.QueryAsync<VoucherDepartmentReport>(
            "dbo.spReport_GetDepartmentPerformance",
            new { Days = NormaliseDays(days) },
            cancellationToken);

    public Task<IReadOnlyList<VoucherOutletReport>> GetOutletsAsync(
        int days,
        CancellationToken cancellationToken = default) =>
        db.QueryAsync<VoucherOutletReport>(
            "dbo.spReport_GetOutletPerformance",
            new { Days = NormaliseDays(days) },
            cancellationToken);

    private static int NormaliseDays(int days) => days is 7 or 30 or 90 ? days : 30;
}

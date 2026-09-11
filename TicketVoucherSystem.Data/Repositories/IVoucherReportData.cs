using TicketVoucherSystem.Data.Models;

namespace TicketVoucherSystem.Data.Repositories;

public interface IVoucherReportData
{
    Task<VoucherReportSummary> GetSummaryAsync(int days, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VoucherTrend>> GetTrendAsync(int days, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VoucherDepartmentReport>> GetDepartmentsAsync(int days, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VoucherOutletReport>> GetOutletsAsync(int days, CancellationToken cancellationToken = default);
}

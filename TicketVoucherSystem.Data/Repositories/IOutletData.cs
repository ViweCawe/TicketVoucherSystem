using TicketVoucherSystem.Data.Models;

namespace TicketVoucherSystem.Data.Repositories;

public interface IOutletData
{
    Task<IReadOnlyList<Outlet>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Outlet>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(int? id, string name, string code, int displayOrder, CancellationToken cancellationToken = default);
    Task SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
}

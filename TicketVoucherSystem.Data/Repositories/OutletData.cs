using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Sql;

namespace TicketVoucherSystem.Data.Repositories;

public sealed class OutletData(IDataAccess db) : IOutletData
{
    public Task<IReadOnlyList<Outlet>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        db.QueryAsync<Outlet>("dbo.spOutlet_GetActive", cancellationToken: cancellationToken);

    public Task<IReadOnlyList<Outlet>> GetAllAsync(CancellationToken cancellationToken = default) =>
        db.QueryAsync<Outlet>("dbo.spOutlet_GetAll", cancellationToken: cancellationToken);

    public async Task SaveAsync(
        int? id,
        string name,
        string code,
        int displayOrder,
        CancellationToken cancellationToken = default) =>
        _ = await db.ExecuteAsync(
            "dbo.spOutlet_Save",
            new { Id = id, Name = name.Trim(), Code = code.Trim().ToUpperInvariant(), DisplayOrder = displayOrder },
            cancellationToken);

    public async Task SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default) =>
        _ = await db.ExecuteAsync("dbo.spOutlet_SetActive", new { Id = id, IsActive = isActive }, cancellationToken);
}

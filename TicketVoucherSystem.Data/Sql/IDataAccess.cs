namespace TicketVoucherSystem.Data.Sql;

public interface IDataAccess
{
    Task<IReadOnlyList<T>> QueryAsync<T>(
        string storedProcedure,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    Task<T?> QuerySingleOrDefaultAsync<T>(
        string storedProcedure,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    Task<int> ExecuteAsync(
        string storedProcedure,
        object? parameters = null,
        CancellationToken cancellationToken = default);
}

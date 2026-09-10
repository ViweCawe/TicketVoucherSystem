using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace TicketVoucherSystemApp.Vouchers.Db;

public sealed class SqlDataAccess(IConfiguration configuration) : IDataAccess
{
    private string ConnectionString =>
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

    public async Task<IReadOnlyList<T>> QueryAsync<T>(
        string storedProcedure,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var command = new CommandDefinition(
            storedProcedure,
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return (await connection.QueryAsync<T>(command)).AsList();
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(
        string storedProcedure,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var command = new CommandDefinition(
            storedProcedure,
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<T>(command);
    }

    public async Task<int> ExecuteAsync(
        string storedProcedure,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var command = new CommandDefinition(
            storedProcedure,
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await connection.ExecuteAsync(command);
    }
}

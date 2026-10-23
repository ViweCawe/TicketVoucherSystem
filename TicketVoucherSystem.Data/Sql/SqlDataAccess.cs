using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using TicketVoucherSystem.Data.Exceptions;

namespace TicketVoucherSystem.Data.Sql;

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

        try
        {
            return (await connection.QueryAsync<T>(command)).AsList();
        }
        catch (SqlException exception) when (exception.Number >= 50000)
        {
            throw new VoucherOperationException(exception.Message, exception);
        }
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

        try
        {
            return await connection.QuerySingleOrDefaultAsync<T>(command);
        }
        catch (SqlException exception) when (exception.Number >= 50000)
        {
            throw new VoucherOperationException(exception.Message, exception);
        }
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

        try
        {
            return await connection.ExecuteAsync(command);
        }
        catch (SqlException exception) when (exception.Number >= 50000)
        {
            throw new VoucherOperationException(exception.Message, exception);
        }
    }
}

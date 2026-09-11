using TicketVoucherSystem.Data.Repositories;
using TicketVoucherSystem.Data.Sql;
using Xunit;

namespace TicketVoucherSystemApp.Tests;

public sealed class VoucherDataTests
{
    [Fact]
    public async Task Issue_uses_the_existing_ticket_number_and_selected_package()
    {
        var database = new RecordingDataAccess();
        var tickets = new TicketVoucherData(database);

        await tickets.IssueAsync(" VIP-123456 ", 7, "issuer@example.com");

        Assert.Equal("dbo.spTicketVoucher_Issue", database.StoredProcedure);
        Assert.Equal("VIP-123456", ReadParameter<string>(database.Parameters, "TicketNumber"));
        Assert.Equal(7, ReadParameter<int>(database.Parameters, "TicketPackageId"));
    }

    [Fact]
    public async Task Redeem_sends_outlet_id_to_the_stored_procedure()
    {
        var database = new RecordingDataAccess();
        var vouchers = new VoucherData(database);

        await vouchers.RedeemAsync("1234567890", "Retail", 42, "operator@example.com");

        Assert.Equal("dbo.spVoucher_Redeem", database.StoredProcedure);
        var outletId = database.Parameters?.GetType().GetProperty("OutletId")?.GetValue(database.Parameters);
        Assert.Equal(42, Assert.IsType<int>(outletId));
        Assert.Null(database.Parameters?.GetType().GetProperty("Location"));
    }

    private static T ReadParameter<T>(object? parameters, string name) =>
        Assert.IsType<T>(parameters?.GetType().GetProperty(name)?.GetValue(parameters));

    private sealed class RecordingDataAccess : IDataAccess
    {
        public string? StoredProcedure { get; private set; }
        public object? Parameters { get; private set; }

        public Task<IReadOnlyList<T>> QueryAsync<T>(string storedProcedure, object? parameters = null, CancellationToken cancellationToken = default)
        {
            StoredProcedure = storedProcedure;
            Parameters = parameters;
            return Task.FromResult<IReadOnlyList<T>>([]);
        }

        public Task<T?> QuerySingleOrDefaultAsync<T>(string storedProcedure, object? parameters = null, CancellationToken cancellationToken = default)
        {
            StoredProcedure = storedProcedure;
            Parameters = parameters;
            return Task.FromResult<T?>(default);
        }

        public Task<int> ExecuteAsync(string storedProcedure, object? parameters = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}

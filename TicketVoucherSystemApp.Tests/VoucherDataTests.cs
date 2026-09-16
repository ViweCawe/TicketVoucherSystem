using System.Text.Json;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;
using TicketVoucherSystem.Data.Sql;
using Xunit;

namespace TicketVoucherSystemApp.Tests;

public sealed class VoucherDataTests
{
    [Fact]
    public async Task Issue_batch_serializes_existing_ticket_numbers_and_selected_package()
    {
        var database = new RecordingDataAccess();
        var tickets = new TicketVoucherData(database);

        await tickets.IssueBatchAsync(new[] { "VIP-123456", "VIP-123457" }, 7, "issuer@example.com");

        Assert.Equal("dbo.spTicketVoucher_IssueBatch", database.StoredProcedure);
        Assert.Equal(7, ReadParameter<int>(database.Parameters, "TicketPackageId"));
        var json = ReadParameter<string>(database.Parameters, "TicketNumbersJson");
        Assert.Equal(new[] { "VIP-123456", "VIP-123457" }, JsonSerializer.Deserialize<string[]>(json));
    }

    [Fact]
    public async Task Save_package_sends_both_optional_benefit_values()
    {
        var database = new RecordingDataAccess();
        var tickets = new TicketVoucherData(database);
        var package = new TicketPackage
        {
            Name = "Group Combo",
            RetailAmount = 100,
            FoodAndBeverageAmount = 200,
            ValidDays = 30
        };

        await tickets.SavePackageAsync(package, "admin@example.com");

        Assert.Equal("dbo.spTicketPackage_Save", database.StoredProcedure);
        Assert.Equal(100m, ReadParameter<decimal>(database.Parameters, "RetailAmount"));
        Assert.Equal(200m, ReadParameter<decimal>(database.Parameters, "FoodAndBeverageAmount"));
    }

    [Fact]
    public async Task Redeem_sends_outlet_id_to_the_stored_procedure()
    {
        var database = new RecordingDataAccess();
        var vouchers = new VoucherData(database);

        await vouchers.RedeemAsync("1234567890", "Retail", 42, "operator@example.com");

        Assert.Equal("dbo.spVoucher_Redeem", database.StoredProcedure);
        Assert.Equal(42, ReadParameter<int>(database.Parameters, "OutletId"));
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

        public Task<int> ExecuteAsync(string storedProcedure, object? parameters = null, CancellationToken cancellationToken = default)
        {
            StoredProcedure = storedProcedure;
            Parameters = parameters;
            return Task.FromResult(0);
        }
    }
}

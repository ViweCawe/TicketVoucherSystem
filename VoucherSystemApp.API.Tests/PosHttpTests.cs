using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TicketVoucherSystem.Data.Exceptions;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;
using Xunit;

namespace VoucherSystemApp.API.Tests;

public sealed class PosHttpTests
{
    [Fact]
    public async Task Lookup_requires_a_token()
    {
        await using var app = new TestApi();
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var response = await client.PostAsJsonAsync("/api/pos/v1/vouchers/lookup", Body());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_credentials_are_rejected()
    {
        await using var app = new TestApi();
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var response = await client.PostAsJsonAsync("/api/auth/token", new { userName = "micros-test", password = new string('x', 40) });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Lookup_previews_300_off_1500_without_redeeming()
    {
        await using var app = new TestApi(); using var client = await app.AuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/pos/v1/vouchers/lookup", Body());
        response.EnsureSuccessStatusCode(); var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("canRedeem").GetBoolean());
        Assert.Equal(300m, json.GetProperty("discountAmount").GetDecimal());
        Assert.Equal(1200m, json.GetProperty("amountDue").GetDecimal());
        Assert.Null(app.Data.LastCommand);
    }

    [Fact]
    public async Task Redeem_uses_authenticated_till_outlet_and_department()
    {
        await using var app = new TestApi(); using var client = await app.AuthenticatedClient();
        var body = Body(); body["outletId"] = 999; body["departmentType"] = "Retail"; body["tillId"] = "FORGED";
        var response = await client.PostAsJsonAsync("/api/pos/v1/vouchers/redeem", body);
        response.EnsureSuccessStatusCode();
        Assert.Equal(7, app.Data.LastCommand!.OutletId);
        Assert.Equal("FoodAndBeverage", app.Data.LastCommand.DepartmentType);
        Assert.Equal("TILL-01", app.Data.LastCommand.TillId);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1200m, json.GetProperty("amountDue").GetDecimal());
    }

    [Theory]
    [InlineData("saleTotal", -1)]
    [InlineData("eligibleTotal", 1501)]
    [InlineData("eligibleTotal", 0)]
    [InlineData("saleTotal", 1500.001)]
    public async Task Invalid_amounts_do_not_reach_database(string field, double value)
    {
        await using var app = new TestApi(); using var client = await app.AuthenticatedClient();
        var body = Body(); body[field] = value;
        var response = await client.PostAsJsonAsync("/api/pos/v1/vouchers/redeem", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Null(app.Data.LastCommand);
    }

    [Theory]
    [InlineData("idempotencyKey", "00000000-0000-0000-0000-000000000000")]
    [InlineData("currency", "USD")]
    [InlineData("operatorId", "")]
    [InlineData("voucherCode", "bad barcode")]
    public async Task Invalid_identifiers_are_rejected(string field, string value)
    {
        await using var app = new TestApi(); using var client = await app.AuthenticatedClient();
        var body = Body(); body[field] = value;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/pos/v1/vouchers/redeem", body)).StatusCode);
        Assert.Null(app.Data.LastCommand);
    }

    [Theory]
    [InlineData(50002, 409)] [InlineData(50003, 409)] [InlineData(50004, 410)]
    [InlineData(50006, 403)] [InlineData(50101, 409)] [InlineData(50103, 422)]
    public async Task Business_errors_have_stable_HTTP_statuses(int error, int status)
    {
        await using var app = new TestApi(); app.Data.Error = error; using var client = await app.AuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/pos/v1/vouchers/redeem", Body());
        Assert.Equal(status, (int)response.StatusCode);
        Assert.DoesNotContain("private database detail", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Below_value_lookup_has_no_discount()
    {
        await using var app = new TestApi(); using var client = await app.AuthenticatedClient();
        var body = Body(); body["eligibleTotal"] = 200m;
        var response = await client.PostAsJsonAsync("/api/pos/v1/vouchers/lookup", body);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("canRedeem").GetBoolean());
        Assert.Equal(0m, json.GetProperty("discountAmount").GetDecimal());
    }

    private static Dictionary<string, object> Body() => new()
    {
        ["voucherCode"] = "TICKET-001", ["saleTotal"] = 1500m, ["eligibleTotal"] = 1500m,
        ["currency"] = "ZAR", ["idempotencyKey"] = Guid.NewGuid(), ["transactionId"] = "SALE-1", ["operatorId"] = "EMP-1"
    };
}

internal sealed class TestApi : WebApplicationFactory<Program>
{
    internal const string Secret = "test-only-machine-credential-1234567890";
    public FakeData Data { get; } = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=unused;Database=unused;Integrated Security=true",
            ["Authentication:Issuer"] = "VoucherSystemApp.API", ["Authentication:Audience"] = "VoucherPOS",
            ["Authentication:SecretKey"] = "test-only-signing-key-12345678901234567890",
            ["Authentication:Clients:0:ClientId"] = "micros-test",
            ["Authentication:Clients:0:SecretHash"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Secret))),
            ["Authentication:Clients:0:TillId"] = "TILL-01", ["Authentication:Clients:0:OutletId"] = "7",
            ["Authentication:Clients:0:DepartmentType"] = "FoodAndBeverage"
        }));
        builder.ConfigureServices(s => { s.RemoveAll<IPosVoucherData>(); s.AddSingleton<IPosVoucherData>(Data); });
    }
    public async Task<HttpClient> AuthenticatedClient()
    {
        var client = CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var response = await client.PostAsJsonAsync("/api/auth/token", new { userName = "micros-test", password = Secret });
        response.EnsureSuccessStatusCode(); var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
        return client;
    }
}

internal sealed class FakeData : IPosVoucherData
{
    public PosRedemptionCommand? LastCommand { get; private set; }
    public int? Error { get; set; }
    public Task<Voucher?> LookupAsync(string code, string departmentType, int outletId, CancellationToken cancellationToken = default)
        => Task.FromResult<Voucher?>(new Voucher { Id = 1, Code = code, DepartmentType = departmentType,
            Amount = 300m, Status = "Issued", ExpiresUtc = DateTime.UtcNow.AddDays(1), PackageName = "Food voucher" });
    public Task<PosRedemption?> RedeemAsync(PosRedemptionCommand command, CancellationToken cancellationToken = default)
    {
        LastCommand = command;
        if (Error.HasValue) throw new VoucherOperationException(Error.Value, "private database detail", new Exception());
        return Task.FromResult<PosRedemption?>(new PosRedemption { Id = 1, SaleTotal = command.SaleTotal,
            DiscountAmount = 300m, AmountDue = command.SaleTotal - 300m, RedeemedUtc = DateTime.UtcNow });
    }
}

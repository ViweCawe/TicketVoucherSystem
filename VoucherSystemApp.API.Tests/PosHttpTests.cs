using System.Net;
using System.Net.Http.Json;
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
    [Theory]
    [InlineData("lookup")][InlineData("redeem")]
    public async Task Endpoints_work_without_credentials(string action)
    {
        await using var app = new TestApi();
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var response = await client.PostAsJsonAsync("/api/pos/v1/vouchers/" + action,
            new { voucherCode = "TICKET-001", departmentType = "FoodAndBeverage" });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(300m, json.GetProperty("voucherAmount").GetDecimal());
        Assert.Equal(action == "lookup", json.GetProperty("isValid").GetBoolean());
    }
    [Theory]
    [InlineData(50002,409)][InlineData(50003,409)][InlineData(50004,410)][InlineData(50001,404)]
    public async Task Unavailable_vouchers_are_rejected(int error, int status)
    {
        await using var app = new TestApi(); app.Data.Error = error;
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(status,(int)(await client.PostAsJsonAsync("/api/pos/v1/vouchers/redeem",
            new { voucherCode = "TICKET-001", departmentType = "Retail" })).StatusCode);
    }
    [Fact]
    public async Task Swagger_is_accessible_without_authentication()
    {
        await using var app = new TestApi(); using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        (await client.GetAsync("/swagger/index.html")).EnsureSuccessStatusCode();
        var json = await client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.DoesNotContain("/api/auth/token", json);
        Assert.Contains("departmentType", json);
    }
    [Fact]
    public async Task Invalid_department_is_rejected()
    {
        await using var app = new TestApi(); using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/pos/v1/vouchers/redeem",new { voucherCode="ABC",departmentType="Other" })).StatusCode);
    }
}
internal sealed class TestApi : WebApplicationFactory<Program>
{
    public FakeData Data { get; } = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=unused;Database=unused;Integrated Security=true",
        }));
        builder.ConfigureServices(s => { s.RemoveAll<IPosVoucherData>(); s.AddSingleton<IPosVoucherData>(Data); });
    }
}

internal sealed class FakeData : IPosVoucherData
{
    public int? Error { get; set; }
    public Task<Voucher?> LookupAsync(string code,string departmentType,CancellationToken cancellationToken=default) =>
        Task.FromResult<Voucher?>(new Voucher { Code=code,DepartmentType=departmentType,Amount=300,Status="Issued",ExpiresUtc=DateTime.UtcNow.AddDays(1) });
    public async Task<Voucher?> RedeemAsync(string code,string departmentType,CancellationToken cancellationToken=default)
    {
        if(Error.HasValue) throw new VoucherOperationException(Error.Value,"database detail",new Exception());
        var voucher=await LookupAsync(code,departmentType,cancellationToken); voucher!.Status="Redeemed"; return voucher;
    }
}

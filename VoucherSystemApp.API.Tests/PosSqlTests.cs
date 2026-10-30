using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using TicketVoucherSystem.Data.Exceptions;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;
using TicketVoucherSystem.Data.Sql;
using Xunit;

namespace VoucherSystemApp.API.Tests;

// Run only against a disposable database deployed from this repository.
[Trait("Category", "SqlServer")]
public sealed class PosSqlTests : IAsyncLifetime
{
    private readonly string connectionString = Environment.GetEnvironmentVariable("POS_TEST_SQL")
        ?? "";
    private readonly string prefix = "TEST-" + Guid.NewGuid().ToString("N")[..16];
    private int outletId, packageId;
    private PosVoucherData Data => new(new SqlDataAccess(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = connectionString }).Build()));

    public async Task InitializeAsync()
    {
        Assert.False(string.IsNullOrWhiteSpace(connectionString), "Set POS_TEST_SQL or run with --filter Category!=SqlServer.");
        await using var db = new SqlConnection(connectionString);
        outletId = await db.QuerySingleAsync<int>("INSERT dbo.Outlet(Name,Code) OUTPUT inserted.Id VALUES (@Name,@Code)", new { Name = prefix, Code = prefix[..20] });
        packageId = await db.QuerySingleAsync<int>("INSERT dbo.VoucherPackage(Name,DepartmentType,Amount,CreatedBy) OUTPUT inserted.Id VALUES (@Name,'FoodAndBeverage',300,'tests')", new { Name = prefix });
    }
    public async Task DisposeAsync()
    {
        if (outletId == 0) return;
        await using var db = new SqlConnection(connectionString);
        await db.ExecuteAsync("DELETE e FROM dbo.VoucherEvent e JOIN dbo.Voucher v ON v.Id=e.VoucherId WHERE v.PackageId=@packageId; DELETE r FROM dbo.PosRedemption r JOIN dbo.Voucher v ON v.Id=r.VoucherId WHERE v.PackageId=@packageId; DELETE dbo.Voucher WHERE PackageId=@packageId; DELETE dbo.VoucherPackage WHERE Id=@packageId; DELETE dbo.Outlet WHERE Id=@outletId;", new { packageId, outletId });
    }
    private async Task<string> Voucher(string status = "Issued", bool expired = false, string department = "FoodAndBeverage")
    {
        var code = "V-" + Guid.NewGuid().ToString("N")[..24];
        await using var db = new SqlConnection(connectionString);
        await db.ExecuteAsync("INSERT dbo.Voucher(PackageId,Code,DepartmentType,Amount,Status,IssuedBy,IssuedUtc,ExpiresUtc) VALUES (@packageId,@code,@department,300,@status,'tests',DATEADD(day,-2,SYSUTCDATETIME()),DATEADD(day,@days,SYSUTCDATETIME()))", new { packageId, code, department, status, days = expired ? -1 : 1 });
        return code;
    }
    private PosRedemptionCommand Command(string code) => new(prefix.ToLowerInvariant(), "TILL-1", outletId,
        "FoodAndBeverage", Guid.NewGuid(), Guid.NewGuid().ToString("N"), "EMP-1", code, 1500m, 1500m, "ZAR");

    [Fact]
    public async Task Redeem_and_retry_return_one_receipt_and_one_audit_event()
    {
        var command = Command(await Voucher());
        var first = await Data.RedeemAsync(command); var retry = await Data.RedeemAsync(command);
        Assert.Equal(300m, first!.DiscountAmount); Assert.Equal(1200m, first.AmountDue);
        Assert.False(first.IsReplay); Assert.True(retry!.IsReplay); Assert.Equal(first.Id, retry.Id);
        await using var db = new SqlConnection(connectionString);
        Assert.Equal(1, await db.QuerySingleAsync<int>("SELECT COUNT(*) FROM dbo.VoucherEvent WHERE VoucherId=@Id AND EventType='Redeemed'", new { Id = first.VoucherId }));
    }
    [Fact]
    public async Task Changed_payload_or_new_key_for_same_sale_is_rejected()
    {
        var command = Command(await Voucher()); await Data.RedeemAsync(command);
        var changed = await Assert.ThrowsAsync<VoucherOperationException>(() => Data.RedeemAsync(command with { SaleTotal = 1600m }));
        Assert.Equal(50101, changed.ErrorCode);
        var newKey = await Assert.ThrowsAsync<VoucherOperationException>(() => Data.RedeemAsync(command with { IdempotencyKey = Guid.NewGuid() }));
        Assert.Equal(50102, newKey.ErrorCode);
    }
    [Fact]
    public async Task Simultaneous_retry_has_one_redemption()
    {
        var command = Command(await Voucher());
        var results = await Task.WhenAll(Data.RedeemAsync(command), Data.RedeemAsync(command));
        Assert.Single(results, r => !r!.IsReplay); Assert.Single(results, r => r!.IsReplay);
        Assert.Equal(results[0]!.Id, results[1]!.Id);
    }
    [Fact]
    public async Task Two_tills_cannot_redeem_same_voucher()
    {
        var command = Command(await Voucher());
        async Task<bool> Try(PosRedemptionCommand c)
        {
            try { await Data.RedeemAsync(c); return true; }
            catch (VoucherOperationException e) when (e.ErrorCode == 50002) { return false; }
        }
        var results = await Task.WhenAll(Try(command), Try(command with { ClientId = command.ClientId + "-other", TillId = "TILL-2" }));
        Assert.Single(results, r => r);
    }
    [Theory]
    [InlineData("Cancelled", false, 50003)] [InlineData("Issued", true, 50004)] [InlineData("Redeemed", false, 50002)]
    public async Task Unavailable_voucher_is_rejected(string status, bool expired, int error)
    {
        var command = Command(await Voucher(status, expired));
        var exception = await Assert.ThrowsAsync<VoucherOperationException>(() => Data.RedeemAsync(command));
        Assert.Equal(error, exception.ErrorCode);
    }
    [Fact]
    public async Task Below_value_is_rejected_without_changing_voucher()
    {
        var command = Command(await Voucher()) with { EligibleTotal = 200m };
        Assert.Equal(50103, (await Assert.ThrowsAsync<VoucherOperationException>(() => Data.RedeemAsync(command))).ErrorCode);
        var voucher = await Data.LookupAsync(command.VoucherCode, command.DepartmentType, outletId);
        Assert.Equal("Issued", voucher!.Status);
    }
    [Fact]
    public async Task Wrong_department_cannot_use_the_voucher()
    {
        var command = Command(await Voucher(department: "Retail"));
        Assert.Null(await Data.LookupAsync(command.VoucherCode, command.DepartmentType, outletId));
        Assert.Equal(50001, (await Assert.ThrowsAsync<VoucherOperationException>(() => Data.RedeemAsync(command))).ErrorCode);
    }
    [Fact]
    public async Task Exact_value_leaves_zero_due()
    {
        var receipt = await Data.RedeemAsync(Command(await Voucher()) with { SaleTotal = 300m, EligibleTotal = 300m });
        Assert.Equal(0m, receipt!.AmountDue);
    }
}

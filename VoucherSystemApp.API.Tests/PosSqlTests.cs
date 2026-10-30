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

    [Fact]
    public async Task Two_simultaneous_redemptions_have_one_winner_and_one_event()
    {
        var code = await Voucher();
        async Task<bool> TryRedeem()
        {
            try { await Data.RedeemAsync(code,"FoodAndBeverage"); return true; }
            catch(VoucherOperationException e) when(e.ErrorCode==50002) { return false; }
        }
        var results=await Task.WhenAll(TryRedeem(),TryRedeem());
        Assert.Single(results,r=>r);
        await using var db=new SqlConnection(connectionString);
        Assert.Equal(1,await db.QuerySingleAsync<int>("SELECT COUNT(*) FROM dbo.VoucherEvent e JOIN dbo.Voucher v ON v.Id=e.VoucherId WHERE v.Code=@code AND e.EventType='Redeemed'",new {code}));
        Assert.Equal("Redeemed",(await Data.LookupAsync(code,"FoodAndBeverage"))!.Status);
    }
    [Theory]
    [InlineData("Cancelled",false,50003)][InlineData("Issued",true,50004)][InlineData("Redeemed",false,50002)]
    public async Task Invalid_voucher_cannot_redeem(string status,bool expired,int expected)
    {
        var code=await Voucher(status,expired);
        Assert.Equal(expected,(await Assert.ThrowsAsync<VoucherOperationException>(()=>Data.RedeemAsync(code,"FoodAndBeverage"))).ErrorCode);
    }
    [Fact]
    public async Task Department_selects_the_correct_benefit()
    {
        var code=await Voucher(department:"Retail");
        Assert.Null(await Data.LookupAsync(code,"FoodAndBeverage"));
        Assert.Equal(50001,(await Assert.ThrowsAsync<VoucherOperationException>(()=>Data.RedeemAsync(code,"FoodAndBeverage"))).ErrorCode);
    }
}

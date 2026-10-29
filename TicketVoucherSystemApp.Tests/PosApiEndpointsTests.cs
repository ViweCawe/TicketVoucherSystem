using Microsoft.AspNetCore.Http;
using TicketVoucherSystem.Data.Exceptions;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;
using TicketVoucherSystemApp.Pos;
using Xunit;

namespace TicketVoucherSystemApp.Tests;

public sealed class PosApiEndpointsTests
{
    [Fact]
    public async Task Redeem_returns_voucher_and_records_pos_operator_context()
    {
        var repository = new StubVoucherData
        {
            Voucher = new Voucher
            {
                Id = 17,
                Code = "1234567890",
                Status = "Redeemed",
                DepartmentType = VoucherDepartments.Retail,
                Amount = 250,
                PackageName = "Retail package",
                RedeemedOutletId = 4,
                RedeemedAt = "Main Shop",
                RedeemedUtc = new DateTime(2026, 10, 22, 10, 30, 0, DateTimeKind.Utc)
            }
        };
        var request = new PosRedemptionRequest(" 1234567890 ", "retail", 4, "TILL-07", "EMP-42");

        var result = await PosApiEndpoints.RedeemAsync(
            request,
            repository,
            NewHttpContext(),
            CancellationToken.None);

        Assert.Equal(
            StatusCodes.Status200OK,
            Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        var response = Assert.IsType<PosRedemptionResponse>(
            Assert.IsAssignableFrom<IValueHttpResult>(result).Value);
        Assert.Equal("redeemed", response.Result);
        Assert.Equal(250m, response.Amount);
        Assert.Equal("ZAR", response.Currency);
        Assert.Equal("POS:TILL-07:EMP-42", repository.UserName);
        Assert.Equal("1234567890", repository.Code);
        Assert.Equal(VoucherDepartments.Retail, repository.DepartmentType);
    }

    [Fact]
    public async Task Redeem_maps_already_redeemed_to_conflict()
    {
        var repository = new StubVoucherData
        {
            Exception = new VoucherOperationException(
                50002,
                "This voucher has already been redeemed.",
                new Exception())
        };

        var result = await PosApiEndpoints.RedeemAsync(
            ValidRequest(),
            repository,
            NewHttpContext(),
            CancellationToken.None);

        Assert.Equal(
            StatusCodes.Status409Conflict,
            Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        var error = Assert.IsType<PosApiError>(
            Assert.IsAssignableFrom<IValueHttpResult>(result).Value);
        Assert.Equal("already_redeemed", error.Code);
    }

    [Fact]
    public async Task Redeem_rejects_invalid_input_without_calling_database()
    {
        var repository = new StubVoucherData();
        var request = ValidRequest() with { VoucherCode = " " };

        var result = await PosApiEndpoints.RedeemAsync(
            request,
            repository,
            NewHttpContext(),
            CancellationToken.None);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        Assert.False(repository.RedeemCalled);
    }

    [Theory]
    [InlineData(50001, StatusCodes.Status404NotFound, "voucher_not_found")]
    [InlineData(50003, StatusCodes.Status409Conflict, "voucher_cancelled")]
    [InlineData(50004, StatusCodes.Status410Gone, "voucher_expired")]
    [InlineData(50006, StatusCodes.Status400BadRequest, "invalid_outlet")]
    public async Task Redeem_maps_database_outcomes_to_stable_pos_errors(
        int databaseError,
        int expectedStatus,
        string expectedCode)
    {
        var repository = new StubVoucherData
        {
            Exception = new VoucherOperationException(databaseError, "Rejected.", new Exception())
        };

        var result = await PosApiEndpoints.RedeemAsync(
            ValidRequest(),
            repository,
            NewHttpContext(),
            CancellationToken.None);

        Assert.Equal(
            expectedStatus,
            Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        var error = Assert.IsType<PosApiError>(
            Assert.IsAssignableFrom<IValueHttpResult>(result).Value);
        Assert.Equal(expectedCode, error.Code);
    }

    private static PosRedemptionRequest ValidRequest() =>
        new("1234567890", VoucherDepartments.Retail, 4, "TILL-07", "EMP-42");

    private static HttpContext NewHttpContext()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "test-trace-id";
        return context;
    }

    private sealed class StubVoucherData : IVoucherData
    {
        public Voucher? Voucher { get; init; }
        public VoucherOperationException? Exception { get; init; }
        public bool RedeemCalled { get; private set; }
        public string? Code { get; private set; }
        public string? DepartmentType { get; private set; }
        public string? UserName { get; private set; }

        public Task<Voucher?> RedeemAsync(
            string code,
            string departmentType,
            int outletId,
            string userName,
            CancellationToken cancellationToken = default)
        {
            RedeemCalled = true;
            Code = code;
            DepartmentType = departmentType;
            UserName = userName;

            if (Exception is not null)
            {
                throw Exception;
            }

            return Task.FromResult(Voucher);
        }

        public Task<IReadOnlyList<Voucher>> GetRecentAsync(
            int count = 10,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Voucher>>([]);

        public Task<IReadOnlyList<Voucher>> SearchAsync(
            string? search,
            string? status,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Voucher>>([]);

        public Task<Voucher?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Voucher?>(null);

        public Task<IReadOnlyList<VoucherEvent>> GetEventsAsync(
            long voucherId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<VoucherEvent>>([]);

        public Task<VoucherDashboard> GetDashboardAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new VoucherDashboard());

        public Task CancelAsync(
            long id,
            string reason,
            string userName,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}

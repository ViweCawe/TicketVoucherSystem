using System.Text.Json;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Sql;

namespace TicketVoucherSystem.Data.Repositories;

public sealed class TicketVoucherData(IDataAccess db) : ITicketVoucherData
{
    public Task<IReadOnlyList<TicketPackage>> GetActivePackagesAsync(CancellationToken cancellationToken = default) =>
        db.QueryAsync<TicketPackage>("dbo.spTicketPackage_GetActive", cancellationToken: cancellationToken);

    public Task<IReadOnlyList<TicketPackage>> GetAllPackagesAsync(CancellationToken cancellationToken = default) =>
        db.QueryAsync<TicketPackage>("dbo.spTicketPackage_GetAll", cancellationToken: cancellationToken);

    public async Task SavePackageAsync(
        TicketPackage package,
        string userName,
        CancellationToken cancellationToken = default) =>
        await db.ExecuteAsync(
            "dbo.spTicketPackage_Save",
            new
            {
                package.Id,
                package.Name,
                package.Description,
                package.RetailAmount,
                package.FoodAndBeverageAmount,
                package.ValidDays,
                UserName = userName
            },
            cancellationToken);

    public async Task SetPackageActiveAsync(
        int id,
        bool isActive,
        string userName,
        CancellationToken cancellationToken = default) =>
        await db.ExecuteAsync(
            "dbo.spTicketPackage_SetActive",
            new { Id = id, IsActive = isActive, UserName = userName },
            cancellationToken);

    public Task<IReadOnlyList<TicketVoucherIssue>> IssueBatchAsync(
        IReadOnlyList<string> ticketNumbers,
        int ticketPackageId,
        string userName,
        CancellationToken cancellationToken = default) =>
        db.QueryAsync<TicketVoucherIssue>(
            "dbo.spTicketVoucher_IssueBatch",
            new
            {
                TicketNumbersJson = JsonSerializer.Serialize(ticketNumbers),
                TicketPackageId = ticketPackageId,
                UserName = userName
            },
            cancellationToken);

    public Task<TicketVoucherIssue?> GetIssueAsync(long id, CancellationToken cancellationToken = default) =>
        db.QuerySingleOrDefaultAsync<TicketVoucherIssue>("dbo.spTicketVoucherIssue_GetById", new { Id = id }, cancellationToken);

    public Task<IReadOnlyList<Voucher>> GetVouchersAsync(long id, CancellationToken cancellationToken = default) =>
        db.QueryAsync<Voucher>("dbo.spTicketVoucherIssue_GetVouchers", new { Id = id }, cancellationToken);

    public Task<IReadOnlyList<TicketVoucherIssue>> GetBatchAsync(Guid batchId, CancellationToken cancellationToken = default) =>
        db.QueryAsync<TicketVoucherIssue>("dbo.spTicketVoucherIssue_GetBatch", new { BatchId = batchId }, cancellationToken);

    public Task<IReadOnlyList<Voucher>> GetBatchVouchersAsync(Guid batchId, CancellationToken cancellationToken = default) =>
        db.QueryAsync<Voucher>("dbo.spTicketVoucherIssue_GetBatchVouchers", new { BatchId = batchId }, cancellationToken);
}

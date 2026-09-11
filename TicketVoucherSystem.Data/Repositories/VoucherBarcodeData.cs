using System.Text.Json;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Sql;

namespace TicketVoucherSystem.Data.Repositories;

public sealed class VoucherBarcodeData(IDataAccess db) : IVoucherBarcodeData
{
    public async Task<BarcodeImportResult> ImportAsync(
        IReadOnlyList<string> codes,
        string userName,
        CancellationToken cancellationToken = default) =>
        await db.QuerySingleOrDefaultAsync<BarcodeImportResult>(
            "dbo.spVoucherBarcode_Import",
            new { CodesJson = JsonSerializer.Serialize(codes), UserName = userName },
            cancellationToken) ?? new BarcodeImportResult();

    public async Task<BarcodeStock> GetStockAsync(CancellationToken cancellationToken = default) =>
        await db.QuerySingleOrDefaultAsync<BarcodeStock>(
            "dbo.spVoucherBarcode_GetStock",
            cancellationToken: cancellationToken) ?? new BarcodeStock();

    public Task<IReadOnlyList<VoucherBarcode>> SearchAsync(
        string? search,
        string? status,
        CancellationToken cancellationToken = default) =>
        db.QueryAsync<VoucherBarcode>(
            "dbo.spVoucherBarcode_Search",
            new { Search = search, Status = status },
            cancellationToken);
}

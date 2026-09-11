using TicketVoucherSystem.Data.Models;

namespace TicketVoucherSystem.Data.Repositories;

public interface IVoucherBarcodeData
{
    Task<BarcodeImportResult> ImportAsync(
        IReadOnlyList<string> codes,
        string userName,
        CancellationToken cancellationToken = default);

    Task<BarcodeStock> GetStockAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VoucherBarcode>> SearchAsync(
        string? search,
        string? status,
        CancellationToken cancellationToken = default);
}

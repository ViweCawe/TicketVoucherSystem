using TicketVoucherSystem.Data.Models;

namespace TicketVoucherSystemApp.Services;

public interface IVoucherBarcodeExporter
{
    byte[] CreateSvg(Voucher voucher);
    byte[] CreateTicketSvg(string ticketNumber);
    byte[] CreateZip(IReadOnlyList<Voucher> vouchers);
}

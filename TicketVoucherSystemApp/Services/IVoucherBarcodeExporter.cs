using TicketVoucherSystemApp.Vouchers.Models;

namespace TicketVoucherSystemApp.Services;

public interface IVoucherBarcodeExporter
{
    byte[] CreateSvg(Voucher voucher);
    byte[] CreateZip(IReadOnlyList<Voucher> vouchers);
}

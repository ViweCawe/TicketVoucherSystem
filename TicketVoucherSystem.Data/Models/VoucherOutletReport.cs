namespace TicketVoucherSystem.Data.Models;

public sealed class VoucherOutletReport
{
    public int? OutletId { get; set; }
    public string OutletName { get; set; } = string.Empty;
    public int RedeemedCount { get; set; }
    public decimal RedeemedValue { get; set; }
    public decimal SharePercent { get; set; }
}

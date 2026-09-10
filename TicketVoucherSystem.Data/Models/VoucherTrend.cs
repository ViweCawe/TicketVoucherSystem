namespace TicketVoucherSystem.Data.Models;

public sealed class VoucherTrend
{
    public DateTime ReportDate { get; set; }
    public int IssuedCount { get; set; }
    public int RedeemedCount { get; set; }
    public decimal IssuedValue { get; set; }
    public decimal RedeemedValue { get; set; }
}

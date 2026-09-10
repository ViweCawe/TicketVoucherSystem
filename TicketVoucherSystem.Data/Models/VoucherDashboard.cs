namespace TicketVoucherSystem.Data.Models;

public sealed class VoucherDashboard
{
    public int IssuedCount { get; set; }
    public int RedeemedCount { get; set; }
    public int ExpiredCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal OutstandingValue { get; set; }
    public decimal RedeemedValueToday { get; set; }
}

namespace TicketVoucherSystem.Data.Models;

public sealed class VoucherReportSummary
{
    public int TotalIssued { get; set; }
    public int TotalRedeemed { get; set; }
    public int TotalOutstanding { get; set; }
    public decimal IssuedValue { get; set; }
    public decimal RedeemedValue { get; set; }
    public decimal OutstandingValue { get; set; }
    public decimal RedemptionRate { get; set; }
    public int IssuedToday { get; set; }
    public int RedeemedToday { get; set; }
}

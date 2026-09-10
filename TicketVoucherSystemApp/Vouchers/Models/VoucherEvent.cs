namespace TicketVoucherSystemApp.Vouchers.Models;

public sealed class VoucherEvent
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventUtc { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

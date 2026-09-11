namespace TicketVoucherSystem.Data.Models;

public sealed class TicketVoucherIssue
{
    public long Id { get; set; }
    public int TicketPackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string PackageDescription { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public DateTime IssuedUtc { get; set; }
    public string IssuedBy { get; set; } = string.Empty;
}

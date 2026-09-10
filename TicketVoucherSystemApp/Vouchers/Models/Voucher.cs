namespace TicketVoucherSystemApp.Vouchers.Models;

public sealed class Voucher
{
    public long Id { get; set; }
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DepartmentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime IssuedUtc { get; set; }
    public string IssuedBy { get; set; } = string.Empty;
    public DateTime ExpiresUtc { get; set; }
    public DateTime? RedeemedUtc { get; set; }
    public string? RedeemedAt { get; set; }
    public string? RedeemedBy { get; set; }
    public string? PairReference { get; set; }
}

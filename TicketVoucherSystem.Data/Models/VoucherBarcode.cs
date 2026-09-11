namespace TicketVoucherSystem.Data.Models;

public sealed class VoucherBarcode
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DepartmentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ImportedUtc { get; set; }
    public string ImportedBy { get; set; } = string.Empty;
    public DateTime? AssignedUtc { get; set; }
    public string? AssignedBy { get; set; }
}

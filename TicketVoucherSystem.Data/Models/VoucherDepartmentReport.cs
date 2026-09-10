namespace TicketVoucherSystem.Data.Models;

public sealed class VoucherDepartmentReport
{
    public string DepartmentType { get; set; } = string.Empty;
    public int IssuedCount { get; set; }
    public int RedeemedCount { get; set; }
    public int OutstandingCount { get; set; }
    public decimal IssuedValue { get; set; }
    public decimal RedeemedValue { get; set; }
}

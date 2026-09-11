namespace TicketVoucherSystem.Data.Models;

public sealed class TicketPackage
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BenefitSummary { get; set; } = string.Empty;
    public int BenefitCount { get; set; }
}

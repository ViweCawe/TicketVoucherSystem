namespace TicketVoucherSystem.Data.Models;

public sealed class BarcodeImportResult
{
    public int ImportedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int AvailableCount { get; set; }
}

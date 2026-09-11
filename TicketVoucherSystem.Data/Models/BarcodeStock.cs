namespace TicketVoucherSystem.Data.Models;

public sealed class BarcodeStock
{
    public int AvailableRetail { get; set; }
    public int AvailableFoodAndBeverage { get; set; }
    public int AssignedRetail { get; set; }
    public int AssignedFoodAndBeverage { get; set; }

    public int TotalAvailable => AvailableRetail + AvailableFoodAndBeverage;
}

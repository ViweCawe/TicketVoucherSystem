namespace TicketVoucherSystem.Data.Models;

// A durable receipt: retries return these saved amounts, never a new redemption.
public sealed class PosRedemption
{
    public long Id { get; set; }
    public Guid IdempotencyKey { get; set; }
    public string ClientId { get; set; } = "";
    public string TransactionId { get; set; } = "";
    public long VoucherId { get; set; }
    public string VoucherCode { get; set; } = "";
    public string DepartmentType { get; set; } = "";
    public int OutletId { get; set; }
    public string TillId { get; set; } = "";
    public string OperatorId { get; set; } = "";
    public decimal SaleTotal { get; set; }
    public decimal EligibleTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal AmountDue { get; set; }
    public string Currency { get; set; } = "ZAR";
    public DateTime RedeemedUtc { get; set; }
    public bool IsReplay { get; set; }
}

public sealed record PosRedemptionCommand(string ClientId, string TillId, int OutletId,
    string DepartmentType, Guid IdempotencyKey, string TransactionId, string OperatorId,
    string VoucherCode, decimal SaleTotal, decimal EligibleTotal, string Currency);

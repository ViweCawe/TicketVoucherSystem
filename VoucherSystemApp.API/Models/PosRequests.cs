using System.ComponentModel.DataAnnotations;

namespace VoucherSystemApp.API.Models;

public class LookupRequest : IValidatableObject
{
    [Required, StringLength(32), RegularExpression("^[A-Za-z0-9-]+$")]
    public string VoucherCode { get; set; } = "";
    [Range(typeof(decimal), "0.01", "9999999999.99")]
    public decimal SaleTotal { get; set; }
    [Range(typeof(decimal), "0.01", "9999999999.99")]
    public decimal EligibleTotal { get; set; }
    [Required, RegularExpression("^ZAR$")]
    public string Currency { get; set; } = "ZAR";

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EligibleTotal > SaleTotal)
            yield return new ValidationResult("EligibleTotal cannot exceed SaleTotal.", [nameof(EligibleTotal)]);
        if (decimal.Round(SaleTotal, 2) != SaleTotal || decimal.Round(EligibleTotal, 2) != EligibleTotal)
            yield return new ValidationResult("Amounts must have at most two decimal places.");
    }
}

public sealed class RedeemRequest : LookupRequest
{
    public Guid IdempotencyKey { get; set; }
    [Required, StringLength(64), RegularExpression("^[A-Za-z0-9-]+$")]
    public string TransactionId { get; set; } = "";
    [Required, StringLength(64), RegularExpression("^[A-Za-z0-9@._-]+$")]
    public string OperatorId { get; set; } = "";

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var error in base.Validate(validationContext)) yield return error;
        if (IdempotencyKey == Guid.Empty)
            yield return new ValidationResult("A non-empty IdempotencyKey is required.", [nameof(IdempotencyKey)]);
    }
}

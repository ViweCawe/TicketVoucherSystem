using System.ComponentModel.DataAnnotations;

namespace TicketVoucherSystem.Data.Models;

public sealed class TicketPackage
{
    public int Id { get; set; }

    [Required, StringLength(60)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal? RetailAmount { get; set; }

    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal? FoodAndBeverageAmount { get; set; }

    [Range(1, 3650)]
    public int ValidDays { get; set; } = 365;

    public bool IsActive { get; set; } = true;
    public string BenefitSummary { get; set; } = string.Empty;
    public int BenefitCount { get; set; }
}

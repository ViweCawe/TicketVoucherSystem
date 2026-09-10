using System.ComponentModel.DataAnnotations;

namespace TicketVoucherSystemApp.Vouchers.Models;

public sealed class VoucherPackage
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string DepartmentType { get; set; } = VoucherDepartments.Retail;

    [Range(0.01, 1_000_000)]
    public decimal Amount { get; set; }

    [Range(1, 3650)]
    public int ValidDays { get; set; } = 365;

    public bool IsActive { get; set; } = true;
}

public static class VoucherDepartments
{
    public const string Retail = "Retail";
    public const string FoodAndBeverage = "FoodAndBeverage";

    public static readonly string[] All = [Retail, FoodAndBeverage];
}

using System.ComponentModel.DataAnnotations;
namespace VoucherSystemApp.API.Models;
public sealed class VoucherRequest
{
    [Required, StringLength(32), RegularExpression("^[A-Za-z0-9-]+$")]
    public string VoucherCode { get; set; } = "";
    [Required, RegularExpression("^(Retail|FoodAndBeverage)$")]
    public string DepartmentType { get; set; } = "";
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TicketVoucherSystemApp.Pages;

public sealed class IndexModel : PageModel
{
    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true
        ? RedirectToPage("/Dashboard/Index")
        : RedirectToPage("/Account/Login", new { area = "Identity" });
}

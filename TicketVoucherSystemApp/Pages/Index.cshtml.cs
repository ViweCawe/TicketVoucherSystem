using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TicketVoucherSystemApp.Pages;

public sealed class IndexModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Operations/Issue");
}

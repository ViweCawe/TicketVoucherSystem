using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TicketVoucherSystemApp.Pages.Operations;

public sealed class ImportModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Operations/Issue");

    public IActionResult OnPost() => RedirectToPage("/Operations/Issue");
}

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TicketVoucherSystemApp.Pages;

public sealed class AccessDeniedModel(UserManager<IdentityUser> userManager) : PageModel
{
    public IReadOnlyList<string> Roles { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is not null)
        {
            Roles = (await userManager.GetRolesAsync(user)).OrderBy(role => role).ToArray();
        }
    }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketVoucherSystemApp.Authorization;
using TicketVoucherSystemApp.Models;
using TicketVoucherSystemApp.Services;

namespace TicketVoucherSystemApp.Pages.Admin.Users;

public sealed class IndexModel(
    IUserAdministration users,
    UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public CreateUserInput NewUser { get; set; } = new();

    public IReadOnlyList<UserSummary> Users { get; private set; } = [];
    public IReadOnlyList<string> AvailableRoles => ApplicationRoles.All;

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Users = await users.GetUsersAsync(cancellationToken);

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        var result = await users.CreateAsync(NewUser.Email, NewUser.Password, NewUser.Role);
        return await CompleteAsync(result, "User created.", cancellationToken);
    }

    public async Task<IActionResult> OnPostRolesAsync(
        string userId,
        string[]? selectedRoles,
        CancellationToken cancellationToken)
    {
        var currentUser = await userManager.GetUserAsync(User);
        selectedRoles ??= [];
        if (currentUser?.Id == userId && !selectedRoles.Contains(ApplicationRoles.Admin, StringComparer.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "You cannot remove your own Admin role.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        var result = await users.SetRolesAsync(userId, selectedRoles);
        return await CompleteAsync(result, "Roles updated. The user must sign in again.", cancellationToken);
    }

    public async Task<IActionResult> OnPostLockAsync(
        string userId,
        bool locked,
        CancellationToken cancellationToken)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser?.Id == userId && locked)
        {
            ModelState.AddModelError(string.Empty, "You cannot lock your own account.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        var result = await users.SetLockedAsync(userId, locked);
        return await CompleteAsync(result, locked ? "User locked." : "User unlocked.", cancellationToken);
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(
        string userId,
        string newPassword,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            ModelState.AddModelError(string.Empty, "Enter a temporary password.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        var result = await users.ResetPasswordAsync(userId, newPassword);
        return await CompleteAsync(result, "Password reset.", cancellationToken);
    }

    private async Task<IActionResult> CompleteAsync(
        IdentityResult result,
        string successMessage,
        CancellationToken cancellationToken)
    {
        if (result.Succeeded)
        {
            StatusMessage = successMessage;
            return RedirectToPage();
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken) =>
        Users = await users.GetUsersAsync(cancellationToken);

    public sealed class CreateUserInput
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = ApplicationRoles.Redeemer;
    }
}

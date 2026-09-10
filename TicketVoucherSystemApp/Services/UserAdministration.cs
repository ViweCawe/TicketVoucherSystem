using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketVoucherSystemApp.Authorization;
using TicketVoucherSystemApp.Models;

namespace TicketVoucherSystemApp.Services;

public sealed class UserAdministration(
    UserManager<IdentityUser> userManager) : IUserAdministration
{
    public async Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);

        var result = new List<UserSummary>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new UserSummary(
                user.Id,
                user.Email ?? user.UserName ?? "Unknown user",
                user.EmailConfirmed,
                user.LockoutEnd > DateTimeOffset.UtcNow,
                roles.OrderBy(role => role).ToArray()));
        }

        return result;
    }

    public async Task<IdentityResult> CreateAsync(string email, string password, string role)
    {
        email = email.Trim();
        if (!ApplicationRoles.All.Contains(role, StringComparer.Ordinal))
        {
            return Failed("The selected role is not valid.");
        }

        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            return created;
        }

        var assigned = await userManager.AddToRoleAsync(user, role);
        if (!assigned.Succeeded)
        {
            await userManager.DeleteAsync(user);
        }

        return assigned;
    }

    public async Task<IdentityResult> SetRolesAsync(string userId, IReadOnlyCollection<string> roles)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Failed("User not found.");
        }

        var selectedRoles = roles
            .Where(role => ApplicationRoles.All.Contains(role, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Contains(ApplicationRoles.Admin) &&
            !selectedRoles.Contains(ApplicationRoles.Admin) &&
            (await userManager.GetUsersInRoleAsync(ApplicationRoles.Admin)).Count <= 1)
        {
            return Failed("The final administrator cannot be demoted.");
        }

        var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles.Except(selectedRoles));
        if (!removeResult.Succeeded)
        {
            return removeResult;
        }

        var addResult = await userManager.AddToRolesAsync(user, selectedRoles.Except(currentRoles));
        if (addResult.Succeeded)
        {
            await userManager.UpdateSecurityStampAsync(user);
        }

        return addResult;
    }

    public async Task<IdentityResult> SetLockedAsync(string userId, bool locked)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Failed("User not found.");
        }

        if (locked &&
            await userManager.IsInRoleAsync(user, ApplicationRoles.Admin) &&
            (await userManager.GetUsersInRoleAsync(ApplicationRoles.Admin)).Count <= 1)
        {
            return Failed("The final administrator cannot be locked.");
        }

        var enabled = await userManager.SetLockoutEnabledAsync(user, true);
        if (!enabled.Succeeded)
        {
            return enabled;
        }

        return await userManager.SetLockoutEndDateAsync(
            user,
            locked ? DateTimeOffset.MaxValue : null);
    }

    public async Task<IdentityResult> ResetPasswordAsync(string userId, string newPassword)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Failed("User not found.");
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        return await userManager.ResetPasswordAsync(user, token, newPassword);
    }

    private static IdentityResult Failed(string description) =>
        IdentityResult.Failed(new IdentityError { Description = description });
}

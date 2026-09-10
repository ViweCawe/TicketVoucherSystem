using Microsoft.AspNetCore.Identity;
using TicketVoucherSystemApp.Models;

namespace TicketVoucherSystemApp.Services;

public interface IUserAdministration
{
    Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<IdentityResult> CreateAsync(string email, string password, string role);
    Task<IdentityResult> SetRolesAsync(string userId, IReadOnlyCollection<string> roles);
    Task<IdentityResult> SetLockedAsync(string userId, bool locked);
    Task<IdentityResult> ResetPasswordAsync(string userId, string newPassword);
}

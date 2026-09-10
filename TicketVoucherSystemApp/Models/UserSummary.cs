namespace TicketVoucherSystemApp.Models;

public sealed record UserSummary(
    string Id,
    string Email,
    bool EmailConfirmed,
    bool IsLocked,
    IReadOnlyList<string> Roles);

namespace TicketVoucherSystemApp.Authorization;

public static class ApplicationRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Executive = "Executive";
    public const string Issuer = "Issuer";
    public const string Redeemer = "Redeemer";

    public static readonly string[] All = [Admin, Manager, Executive, Issuer, Redeemer];
}

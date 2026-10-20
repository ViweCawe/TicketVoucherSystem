namespace TicketVoucherSystemApp.Integrations.WebTickets;

public sealed class WebTicketsOptions
{
    public const string SectionName = "WebTickets";
    public const string UserSecretsId = "1149a7eb-093d-4cd4-8639-f5e1727c0a7f";
    public string BaseUrl { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public string CountryCode { get; set; } = "ZA";
    public string EligibleTicketType { get; set; } = "Packages";
    public int TimeoutSeconds { get; set; } = 15;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? BearerToken { get; set; }
    public string? ApiKeyHeaderName { get; set; }
    public string? ApiKey { get; set; }
}

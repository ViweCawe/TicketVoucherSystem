namespace TicketVoucherSystemApp.Integrations.WebTickets;

public sealed class WebTicketsOptions
{
    public const string SectionName = "WebTickets";
    public string Endpoint { get; set; } = "https://test.webtickets.co.za/api/transaction/cableway/lookup";
    public string EligibleTicketType { get; set; } = "Packages";
    public int TimeoutSeconds { get; set; } = 15;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? BearerToken { get; set; }
    public string? ApiKeyHeaderName { get; set; }
    public string? ApiKey { get; set; }
}

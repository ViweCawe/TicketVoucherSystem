namespace TicketVoucherSystemApp.Pos;

public sealed class PosApiOptions
{
    public const string SectionName = "PosApi";
    public const string HeaderName = "X-POS-API-Key";

    public string ApiKey { get; set; } = string.Empty;
}

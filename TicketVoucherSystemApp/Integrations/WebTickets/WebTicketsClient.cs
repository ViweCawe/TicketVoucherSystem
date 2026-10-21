using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TicketVoucherSystemApp.Integrations.WebTickets;

public interface IWebTicketsClient
{
    Task<WebTicketsLookup> LookupAsync(string barcode, CancellationToken cancellationToken = default);
}

public sealed class WebTicketsClient : IWebTicketsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly WebTicketsOptions _options;
    private readonly ILogger<WebTicketsClient> _logger;

    public WebTicketsClient(
        HttpClient httpClient,
        IOptions<WebTicketsOptions> options,
        ILogger<WebTicketsClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 5, 60));
    }

    public async Task<WebTicketsLookup> LookupAsync(string barcode, CancellationToken cancellationToken = default)
    {
        barcode = barcode.Trim();
        if (barcode.Length is < 6 or > 32 ||
            barcode.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            throw new WebTicketsLookupException(
                "Enter a valid ticket barcode containing 6 to 32 letters, numbers, or hyphens.");
        }

        var endpoint = ResolveEndpoint();

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(new WebTicketsLookupRequest(barcode))
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        AddAuthentication(request);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new WebTicketsLookupException("Webtickets did not respond in time.");
        }
        catch (HttpRequestException)
        {
            throw new WebTicketsLookupException("Webtickets could not be reached.");
        }

        using (response)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Webtickets host {Host} returned HTTP {StatusCode} with content type {ContentType}.",
                    endpoint.Host,
                    (int)response.StatusCode,
                    response.Content.Headers.ContentType?.ToString() ?? "unknown");

                throw new WebTicketsLookupException(
                    FormatUpstreamFailure(endpoint, response, responseBody));
            }

            WebTicketsBasket? basket;
            try
            {
                basket = JsonSerializer.Deserialize<WebTicketsBasket>(responseBody, JsonOptions);
            }
            catch (JsonException)
            {
                throw new WebTicketsLookupException(
                    $"Webtickets host '{endpoint.Host}' returned HTTP {(int)response.StatusCode}, " +
                    "but the response was not valid ticket JSON.");
            }

            if (basket is null)
                throw new WebTicketsLookupException(
                    $"Webtickets host '{endpoint.Host}' returned an empty response.");

            var matches = basket.Transactions.SelectMany(x => x.Tickets)
                .Where(x => string.Equals(x.Barcode, barcode, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1)
                throw new WebTicketsLookupException(matches.Length == 0
                    ? "The returned basket does not contain the scanned barcode."
                    : "Webtickets returned the scanned barcode more than once.");

            var ticket = matches[0];
            if (!basket.Authorized || !ticket.Authorized)
                throw new WebTicketsLookupException("This Webtickets purchase is not authorized.");
            return new WebTicketsLookup(basket, ticket);
        }
    }

    private Uri ResolveEndpoint()
    {
        const string lookupPath = "/api/transaction/cableway/lookup";
        var hasExplicitEndpoint = !string.IsNullOrWhiteSpace(_options.Endpoint);
        var value = hasExplicitEndpoint ? _options.Endpoint : _options.BaseUrl;

        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var configuredUri) ||
            configuredUri.Scheme is not ("http" or "https"))
        {
            throw new WebTicketsLookupException(
                "The Webtickets endpoint is not configured. Set WebTickets:BaseUrl in the deployed application.");
        }

        if (hasExplicitEndpoint ||
            string.Equals(configuredUri.AbsolutePath.TrimEnd('/'), lookupPath, StringComparison.OrdinalIgnoreCase))
        {
            return configuredUri;
        }

        return new Uri(configuredUri, lookupPath);
    }

    private static string FormatUpstreamFailure(
        Uri endpoint,
        HttpResponseMessage response,
        string responseBody)
    {
        var message =
            $"Webtickets host '{endpoint.Host}' returned HTTP {(int)response.StatusCode} {response.ReasonPhrase}.";

        if (string.IsNullOrWhiteSpace(responseBody))
            return message;

        var detail = string.Join(
            ' ',
            responseBody.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries));
        if (detail.Length > 300)
            detail = $"{detail[..300]}...";

        return $"{message} Response: {detail}";
    }

    private void AddAuthentication(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_options.BearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.BearerToken);
        }
        else if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password))
        {
            var value = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", value);
        }
        else if (!string.IsNullOrWhiteSpace(_options.ApiKeyHeaderName) && !string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.TryAddWithoutValidation(_options.ApiKeyHeaderName, _options.ApiKey);
        }
        else
        {
            throw new WebTicketsLookupException(
                "Webtickets authentication is not configured in the deployed application.");
        }
    }
}

public sealed class WebTicketsLookupException(string message) : Exception(message);
public sealed record WebTicketsLookup(WebTicketsBasket Basket, WebTicketsTicket Ticket);
public sealed record WebTicketsLookupRequest(string Barcode);

public sealed class WebTicketsBasket
{
    public long BasketId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreateDate { get; set; }
    public bool Authorized { get; set; }
    public List<WebTicketsTransaction> Transactions { get; set; } = [];
}

public sealed class WebTicketsTransaction
{
    public long TransactionId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public decimal TransactionAmount { get; set; }
    public List<WebTicketsTicket> Tickets { get; set; } = [];
}

public sealed class WebTicketsTicket
{
    public long TicketId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public long TicketTypeId { get; set; }
    public string TicketType { get; set; } = string.Empty;
    public decimal TicketPrice { get; set; }
    public long PerformanceId { get; set; }
    public string Performance { get; set; } = string.Empty;
    public bool Authorized { get; set; }
    public bool Validated { get; set; }
}

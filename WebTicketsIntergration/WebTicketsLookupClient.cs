using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;

namespace WebTicketsIntegration;

public interface IWebTicketsLookupClient
{
    Task<WebTicketsUpstreamResponse> LookupAsync(
        string barcode,
        CancellationToken cancellationToken = default);
}

public sealed class WebTicketsLookupClient : IWebTicketsLookupClient
{
    private const string LookupPath = "/api/transaction/cableway/lookup";
    private readonly HttpClient _httpClient;
    private readonly WebTicketsOptions _options;

    public WebTicketsLookupClient(HttpClient httpClient, IOptions<WebTicketsOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 5, 60));
    }

    public async Task<WebTicketsUpstreamResponse> LookupAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var endpoint = ResolveEndpoint();
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(new { barcode })
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        AddAuthentication(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        return new WebTicketsUpstreamResponse(
            (int)response.StatusCode,
            response.Content.Headers.ContentType?.ToString(),
            body,
            endpoint);
    }

    private Uri ResolveEndpoint()
    {
        var hasExplicitEndpoint = !string.IsNullOrWhiteSpace(_options.Endpoint);
        var value = hasExplicitEndpoint ? _options.Endpoint : _options.BaseUrl;

        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var configuredUri) ||
            configuredUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException(
                "Set WebTickets:BaseUrl or WebTickets:Endpoint to the Webtickets lookup URL.");
        }

        if (hasExplicitEndpoint ||
            string.Equals(configuredUri.AbsolutePath.TrimEnd('/'), LookupPath, StringComparison.OrdinalIgnoreCase))
        {
            return configuredUri;
        }

        return new Uri(configuredUri, LookupPath);
    }

    private void AddAuthentication(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_options.BearerToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.BearerToken);
            return;
        }

        if (!string.IsNullOrWhiteSpace(_options.Username) &&
            !string.IsNullOrWhiteSpace(_options.Password))
        {
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password}"));
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
            return;
        }

        if (!string.IsNullOrWhiteSpace(_options.ApiKeyHeaderName) &&
            !string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.TryAddWithoutValidation(_options.ApiKeyHeaderName, _options.ApiKey);
            return;
        }

        throw new InvalidOperationException(
            "Configure Webtickets authentication before performing a lookup.");
    }
}

public sealed record WebTicketsUpstreamResponse(
    int StatusCode,
    string? ContentType,
    string Body,
    Uri Endpoint);

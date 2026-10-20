using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using WebTicketsIntegration;
using Xunit;

namespace TicketVoucherSystemApp.Tests;

public sealed class WebTicketsIntegrationApiClientTests
{
    [Fact]
    public async Task Lookup_sends_the_barcode_to_the_configured_production_endpoint()
    {
        var handler = new RecordingHandler();
        var client = new WebTicketsLookupClient(
            new HttpClient(handler),
            Options.Create(new WebTicketsOptions
            {
                BaseUrl = "https://www.webtickets.co.za/api/transaction/cableway/lookup",
                Username = "user",
                Password = "password"
            }));

        var result = await client.LookupAsync("1605729254");

        Assert.Equal(404, result.StatusCode);
        Assert.Equal(
            "https://www.webtickets.co.za/api/transaction/cableway/lookup",
            handler.RequestUri?.ToString());
        Assert.Equal("Basic", handler.AuthorizationScheme);
        Assert.Contains("\"barcode\":\"1605729254\"", handler.RequestBody);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(
                    "{\"message\":\"not found\"}",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}

using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TicketVoucherSystemApp.Integrations.WebTickets;
using Xunit;

namespace TicketVoucherSystemApp.Tests;

public sealed class WebTicketsClientTests
{
    [Fact]
    public async Task Lookup_selects_the_exact_barcode_from_the_returned_basket()
    {
        const string json = """
        {"basketId":10,"reference":"AA-1","customerEmail":"buyer@example.com","customerName":"Test Buyer","authorized":true,"transactions":[{
          "tickets":[
            {"ticketId":12,"barcode":"OTHER123","ticketType":"Packages","authorized":true},
            {"ticketId":13,"barcode":"MATCH123","ticketType":"Packages","ticketPrice":120,"performance":"09:00 - 10:00","authorized":true}
          ]}]}
        """;
        var handler = new StubHandler(HttpStatusCode.OK, json);
        var result = await CreateClient(handler).LookupAsync("match123");

        Assert.Equal(13, result.Ticket.TicketId);
        Assert.Equal("MATCH123", result.Ticket.Barcode);
        Assert.Equal("Test Buyer", result.Basket.CustomerName);
        Assert.Equal("buyer@example.com", result.Basket.CustomerEmail);
        Assert.Equal("09:00 - 10:00", result.Ticket.Performance);
        Assert.Equal("Basic", handler.AuthorizationScheme);
        Assert.Equal(
            "https://www.webtickets.co.za/api/transaction/cableway/lookup",
            handler.RequestUri?.ToString());
    }

    [Fact]
    public async Task Lookup_accepts_the_full_lookup_url_as_base_url()
    {
        var handler = new StubHandler(
            HttpStatusCode.NotFound,
            """{"message":"Production route is unavailable"}""");

        var exception = await Assert.ThrowsAsync<WebTicketsLookupException>(() =>
            CreateClient(
                handler,
                "https://www.webtickets.co.za/api/transaction/cableway/lookup")
            .LookupAsync("1605729254"));

        Assert.Equal(
            "https://www.webtickets.co.za/api/transaction/cableway/lookup",
            handler.RequestUri?.ToString());
        Assert.Contains("www.webtickets.co.za", exception.Message);
        Assert.Contains("HTTP 404", exception.Message);
        Assert.Contains("Production route is unavailable", exception.Message);
    }

    [Fact]
    public async Task Lookup_accepts_any_authorized_ticket_type()
    {
        const string json = """
        {"authorized":true,"transactions":[{"tickets":[
          {"barcode":"MATCH123","ticketType":"Standard","authorized":true}
        ]}]}
        """;
        var result = await CreateClient(new StubHandler(HttpStatusCode.OK, json))
            .LookupAsync("MATCH123");

        Assert.Equal("Standard", result.Ticket.TicketType);
    }

    private static WebTicketsClient CreateClient(
        HttpMessageHandler handler,
        string baseUrl = "https://www.webtickets.co.za") =>
        new(
            new HttpClient(handler),
            Options.Create(new WebTicketsOptions
            {
                BaseUrl = baseUrl,
                Username = "user",
                Password = "password"
            }),
            NullLogger<WebTicketsClient>.Instance);

    private sealed class StubHandler(HttpStatusCode statusCode, string response) : HttpMessageHandler
    {
        public string? AuthorizationScheme { get; private set; }
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            });
        }
    }
}

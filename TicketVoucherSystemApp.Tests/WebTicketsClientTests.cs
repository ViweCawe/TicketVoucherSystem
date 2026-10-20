using System.Net;
using System.Text;
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
        {"basketId":10,"reference":"AA-1","authorized":true,"transactions":[{
          "tickets":[
            {"ticketId":12,"barcode":"OTHER123","ticketType":"Packages","authorized":true},
            {"ticketId":13,"barcode":"MATCH123","ticketType":"Packages","ticketPrice":120,"authorized":true}
          ]}]}
        """;
        var handler = new StubHandler(HttpStatusCode.OK, json);
        var result = await CreateClient(handler).LookupAsync("match123");

        Assert.Equal(13, result.Ticket.TicketId);
        Assert.Equal("MATCH123", result.Ticket.Barcode);
        Assert.Equal("Basic", handler.AuthorizationScheme);
        Assert.Equal(
            "https://www.webtickets.co.za/api/transaction/cableway/lookup",
            handler.RequestUri?.ToString());
    }

    [Fact]
    public async Task Lookup_rejects_a_non_package_ticket()
    {
        const string json = """
        {"authorized":true,"transactions":[{"tickets":[
          {"barcode":"MATCH123","ticketType":"Standard","authorized":true}
        ]}]}
        """;
        var exception = await Assert.ThrowsAsync<WebTicketsLookupException>(
            () => CreateClient(new StubHandler(HttpStatusCode.OK, json)).LookupAsync("MATCH123"));

        Assert.Contains("not eligible", exception.Message);
    }

    private static WebTicketsClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), Options.Create(new WebTicketsOptions
        {
            BaseUrl = "https://www.webtickets.co.za",
            Username = "user",
            Password = "password"
        }));

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

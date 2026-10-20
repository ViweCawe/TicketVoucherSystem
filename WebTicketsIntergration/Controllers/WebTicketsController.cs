using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace WebTicketsIntegration.Controllers;

[ApiController]
[Route("api/webtickets")]
public sealed class WebTicketsController(
    IWebTicketsLookupClient client,
    IConfiguration configuration,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("lookup")]
    [ProducesResponseType<WebTicketsLookupDiagnostic>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Lookup(
        [FromBody] WebTicketsLookupRequest request,
        [FromHeader(Name = "X-Integration-Key")] string? integrationKey,
        CancellationToken cancellationToken)
    {
        var authorizationFailure = AuthorizeDiagnosticRequest(integrationKey);
        if (authorizationFailure is not null)
            return authorizationFailure;

        try
        {
            var upstream = await client.LookupAsync(request.Barcode.Trim(), cancellationToken);
            object? responseBody = ParseBody(upstream.Body);

            return StatusCode(upstream.StatusCode, new WebTicketsLookupDiagnostic(
                upstream.StatusCode,
                upstream.Endpoint.ToString(),
                upstream.ContentType,
                responseBody));
        }
        catch (InvalidOperationException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Webtickets integration is not configured",
                detail: exception.Message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "Webtickets lookup timed out");
        }
        catch (HttpRequestException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Webtickets could not be reached",
                detail: exception.Message);
        }
    }

    private IActionResult? AuthorizeDiagnosticRequest(string? suppliedKey)
    {
        var expectedKey = configuration["WebTicketsApi:ApiKey"];
        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            return environment.IsDevelopment()
                ? null
                : Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Integration API access key is not configured");
        }

        if (string.IsNullOrEmpty(suppliedKey) ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedKey),
                Encoding.UTF8.GetBytes(suppliedKey)))
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "A valid X-Integration-Key header is required"
            });
        }

        return null;
    }

    private static object? ParseBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return body;
        }
    }
}

public sealed record WebTicketsLookupRequest(
    [property: Required, StringLength(32, MinimumLength = 6)] string Barcode);

public sealed record WebTicketsLookupDiagnostic(
    int UpstreamStatus,
    string Endpoint,
    string? ContentType,
    object? Response);

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace TicketVoucherSystemApp.Pos;

public sealed class PosApiKeyFilter(IOptions<PosApiOptions> options) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var traceId = context.HttpContext.TraceIdentifier;
        var configuredKey = options.Value.ApiKey;

        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            return Results.Json(
                new PosApiError(
                    "pos_api_unavailable",
                    "POS redemption is not configured.",
                    traceId),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var suppliedKey = context.HttpContext.Request.Headers[PosApiOptions.HeaderName].ToString();
        if (!ApiKeysMatch(suppliedKey, configuredKey))
        {
            context.HttpContext.Response.Headers.WWWAuthenticate = "ApiKey";
            return Results.Json(
                new PosApiError(
                    "unauthorized",
                    $"Supply a valid {PosApiOptions.HeaderName} header.",
                    traceId),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return await next(context);
    }

    internal static bool ApiKeysMatch(string suppliedKey, string configuredKey)
    {
        if (string.IsNullOrEmpty(suppliedKey))
        {
            return false;
        }

        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedKey));
        var configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
        return CryptographicOperations.FixedTimeEquals(suppliedHash, configuredHash);
    }
}

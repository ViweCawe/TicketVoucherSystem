using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace VoucherSystemApp.API.Authentication;

public sealed class BasicAuthCredentials
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool IsValid() => !string.IsNullOrWhiteSpace(Username) && Username.Length <= 128
        && !Username.Contains(':') && !Username.Any(char.IsControl)
        && !string.IsNullOrWhiteSpace(Password) && Password.Length <= 1024;
}

// A shared integration credential, not an admin-app Identity login.
public sealed class BasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, IOptions<BasicAuthCredentials> credentials)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.IsHttps) return Result(AuthenticateResult.Fail("HTTPS is required."));
        if (!Request.Headers.TryGetValue("Authorization", out var values))
            return Result(AuthenticateResult.NoResult());
        if (values.Count != 1 || values[0]!.Length > 8192 ||
            !AuthenticationHeaderValue.TryParse(values[0], out var header) ||
            !string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(header.Parameter))
            return Result(AuthenticateResult.Fail("Invalid credentials."));
        try
        {
            var decoded = new UTF8Encoding(false, true).GetString(Convert.FromBase64String(header.Parameter));
            var separator = decoded.IndexOf(':');
            if (separator < 1) return Result(AuthenticateResult.Fail("Invalid credentials."));
            var settings = credentials.Value;
            // Evaluate both comparisons; no password/credential logging.
            var valid = Same(decoded[..separator], settings.Username)
                & Same(decoded[(separator + 1)..], settings.Password);
            if (!valid) return Result(AuthenticateResult.Fail("Invalid credentials."));
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "pos-integration"), new Claim(ClaimTypes.Name, settings.Username)],
                Scheme.Name);
            return Result(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
        catch (Exception ex) when (ex is FormatException or DecoderFallbackException)
        {
            return Result(AuthenticateResult.Fail("Invalid credentials."));
        }
    }
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = "Basic realm=\"Voucher POS API\", charset=\"UTF-8\"";
        Response.Headers.CacheControl = "no-store";
        return Task.CompletedTask;
    }
    private static bool Same(string actual, string expected) => CryptographicOperations.FixedTimeEquals(
        SHA256.HashData(Encoding.UTF8.GetBytes(actual)), SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
    private static Task<AuthenticateResult> Result(AuthenticateResult result) => Task.FromResult(result);
}

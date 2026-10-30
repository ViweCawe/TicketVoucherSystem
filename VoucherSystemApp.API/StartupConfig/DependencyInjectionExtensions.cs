using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using TicketVoucherSystem.Data.Repositories;
using TicketVoucherSystem.Data.Sql;
using VoucherSystemApp.API.Authentication;

namespace VoucherSystemApp.API.StartupConfig;

public static class DependencyInjectionExtensions
{
    public static void AddPosServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();
        builder.Services.AddScoped<IDataAccess, SqlDataAccess>();
        builder.Services.AddScoped<IPosVoucherData, PosVoucherData>();
        builder.Services.AddOptions<PosAuthenticationOptions>()
            .Bind(builder.Configuration.GetSection("Authentication"))
            .Validate(o => o.IsValid(), "Configure a strong signing key and valid, unique POS clients.")
            .ValidateOnStart();
        builder.Services.AddOptions<PosDatabaseOptions>()
            .Bind(builder.Configuration.GetSection("ConnectionStrings"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.DefaultConnection),
                "Configure ConnectionStrings:DefaultConnection for the voucher database.").ValidateOnStart();
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<PosAuthenticationOptions>>((options, configured) =>
        {
            var auth = configured.Value;
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
                ValidateIssuerSigningKey = true, RequireExpirationTime = true, RequireSignedTokens = true,
                ValidIssuer = auth.Issuer, ValidAudience = auth.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.SecretKey)),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256], ClockSkew = TimeSpan.FromSeconds(30)
            };
            options.Events = new JwtBearerEvents
            {
                // Also reject tokens from a disabled or reconfigured till before they expire.
                OnTokenValidated = context =>
                {
                    var user = context.Principal!;
                    var client = auth.Clients.SingleOrDefault(c => c.ClientId == user.FindFirst("sub")?.Value);
                    if (client is null || user.FindFirst("till_id")?.Value != client.TillId
                        || user.FindFirst("outlet_id")?.Value != client.OutletId.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        || user.FindFirst("department")?.Value != client.DepartmentType)
                        context.Fail("The POS client configuration has changed.");
                    return Task.CompletedTask;
                }
            };
        });
        builder.Services.AddAuthorization(options => options.AddPolicy("Pos", policy => policy
            .RequireAuthenticatedUser().RequireClaim("sub").RequireClaim("till_id")
            .RequireClaim("outlet_id").RequireClaim("department", "Retail", "FoodAndBeverage")));
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("token", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.AddPolicy("pos", context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirst("sub")?.Value ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
    }
}

public sealed class PosDatabaseOptions
{
    public string DefaultConnection { get; set; } = "";
}

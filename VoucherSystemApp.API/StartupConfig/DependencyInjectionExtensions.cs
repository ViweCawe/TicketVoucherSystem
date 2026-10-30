using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi;
using VoucherSystemApp.API.Authentication;
using TicketVoucherSystem.Data.Repositories;
using TicketVoucherSystem.Data.Sql;
namespace VoucherSystemApp.API.StartupConfig;
public static class DependencyInjectionExtensions
{
    public static void AddStandardServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http, Scheme = "basic",
                Description = "Enter the configured POS integration username and password."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Basic", document)] = []
            });
        });
        builder.Services.AddProblemDetails();
    }
    public static void AddBasicAuthServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<BasicAuthCredentials>()
            .Bind(builder.Configuration.GetSection("BasicAuth"))
            .Validate(c => c.IsValid(), "Configure BasicAuth:Username and BasicAuth:Password outside source control.")
            .ValidateOnStart();
        builder.Services.AddAuthentication("Basic")
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", _ => { });
        builder.Services.AddAuthorization();
    }
    public static void AddPosServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IDataAccess, SqlDataAccess>();
        builder.Services.AddScoped<IPosVoucherData, PosVoucherData>();
    }
    public static void AddHealthCheckServices(this WebApplicationBuilder builder) =>
        builder.Services.AddHealthChecks().AddSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=localhost;Database=TicketVoucherSystem;Integrated Security=true");
}

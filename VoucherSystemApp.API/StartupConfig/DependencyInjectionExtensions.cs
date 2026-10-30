using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using TicketVoucherSystem.Data.Repositories;
using TicketVoucherSystem.Data.Sql;
using VoucherSystemApp.API.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace VoucherSystemApp.API.StartupConfig;

public static class DependencyInjectionExtensions
{

    public static void AddStandardServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
    }
    public static void AddPosServices(this WebApplicationBuilder builder)
    {

        builder.Services.AddScoped<IDataAccess, SqlDataAccess>();
        builder.Services.AddScoped<IPosVoucherData, PosVoucherData>();
    }
    public static void AddHealthCheckServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!);
    }
    public static void AddAuthServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication("Bearer")
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = builder.Configuration.GetValue<string>("Authentication:Audince"),
                    ValidIssuer = builder.Configuration.GetValue<string>("Authentication:Issuer"),
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration.GetValue<string>("Authentication:SecreteKrey")!))
                };
            });

        builder.Services.AddAuthorization(opts =>
            opts.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build()
        );
    }
}
     
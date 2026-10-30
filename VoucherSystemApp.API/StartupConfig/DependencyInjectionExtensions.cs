using TicketVoucherSystem.Data.Repositories;
using TicketVoucherSystem.Data.Sql;
namespace VoucherSystemApp.API.StartupConfig;
public static class DependencyInjectionExtensions
{
    public static void AddStandardServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddProblemDetails();
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

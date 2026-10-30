using VoucherSystemApp.API.StartupConfig;

var builder = WebApplication.CreateBuilder(args);
builder.AddPosServices();
var app = builder.Build();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "running" })).AllowAnonymous();
app.Run();

// Used by the HTTP integration tests.
public partial class Program { }

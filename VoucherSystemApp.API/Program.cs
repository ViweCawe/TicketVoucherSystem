using VoucherSystemApp.API.StartupConfig;
var builder = WebApplication.CreateBuilder(args);
builder.AddStandardServices();
builder.AddPosServices();
builder.AddBasicAuthServices();
builder.AddHealthCheckServices();
var app = builder.Build();
app.UseExceptionHandler();
app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").RequireAuthorization();
app.Run();
public partial class Program { }

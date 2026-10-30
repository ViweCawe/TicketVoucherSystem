using VoucherSystemApp.API.StartupConfig;
var builder = WebApplication.CreateBuilder(args);
builder.AddStandardServices();
builder.AddPosServices();
builder.AddHealthCheckServices();
var app = builder.Build();
app.UseExceptionHandler();
app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
public partial class Program { }

using VoucherSystemApp.API.StartupConfig;

var builder = WebApplication.CreateBuilder(args);
builder.AddPosServices();
builder.AddAuthServices();
builder.AddHealthCheckServices();
builder.AddStandardServices();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("health").AllowAnonymous();
app.Run();



using WebTicketsIntegration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<WebTicketsOptions>(
    builder.Configuration.GetSection(WebTicketsOptions.SectionName));
builder.Services.AddHttpClient<IWebTicketsLookupClient, WebTicketsLookupClient>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

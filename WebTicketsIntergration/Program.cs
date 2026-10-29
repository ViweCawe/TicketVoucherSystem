<<<<<<< HEAD
using TicketVoucherSystem.Data.Repositories;
using TicketVoucherSystem.Data.Sql;
=======
using WebTicketsIntegration;
>>>>>>> 10219ca0914825c2eef5be925893970e01ed2d46

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
<<<<<<< HEAD
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IDataAccess, SqlDataAccess>();
builder.Services.AddScoped<ITicketVoucherData, TicketVoucherData>();
=======
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<WebTicketsOptions>(
    builder.Configuration.GetSection(WebTicketsOptions.SectionName));
builder.Services.AddHttpClient<IWebTicketsLookupClient, WebTicketsLookupClient>();

>>>>>>> 10219ca0914825c2eef5be925893970e01ed2d46
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
<<<<<<< HEAD
    app.MapOpenApi();
=======
>>>>>>> 10219ca0914825c2eef5be925893970e01ed2d46
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

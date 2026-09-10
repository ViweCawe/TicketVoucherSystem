using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketVoucherSystemApp.Data;
using TicketVoucherSystemApp.Services;
using TicketVoucherSystem.Data.Repositories;
using TicketVoucherSystem.Data.Sql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<IDataAccess, SqlDataAccess>();
builder.Services.AddScoped<IVoucherData, VoucherData>();
builder.Services.AddScoped<IVoucherPackageData, VoucherPackageData>();
builder.Services.AddSingleton<IVoucherBarcodeExporter, VoucherBarcodeExporter>();
builder.Services.AddHostedService<AdminRoleInitializer>();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Operations");
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
app.MapRazorPages().WithStaticAssets();

app.Run();

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketVoucherSystem.Data.Repositories;
using TicketVoucherSystem.Data.Sql;
using TicketVoucherSystemApp.Authorization;
using TicketVoucherSystemApp.Data;
using TicketVoucherSystemApp.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/AccessDenied";
    options.LoginPath = "/Identity/Account/Login";
});

builder.Services.AddScoped<IDataAccess, SqlDataAccess>();
builder.Services.AddScoped<IVoucherData, VoucherData>();
builder.Services.AddScoped<IVoucherPackageData, VoucherPackageData>();
builder.Services.AddScoped<IVoucherReportData, VoucherReportData>();
builder.Services.AddScoped<IUserAdministration, UserAdministration>();
builder.Services.AddSingleton<IVoucherBarcodeExporter, VoucherBarcodeExporter>();
builder.Services.AddHostedService<AdminRoleInitializer>();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Dashboard", "AppUser");
    options.Conventions.AuthorizePage("/Operations/Issue", "VoucherIssueAccess");
    options.Conventions.AuthorizePage("/Operations/Import", "VoucherIssueAccess");
    options.Conventions.AuthorizePage("/Operations/Redeem", "VoucherRedeemAccess");
    options.Conventions.AuthorizeFolder("/Reports", "ReportsAccess");
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AuthorizeAreaPage("Identity", "/Account/Register", "RegistrationDisabled");
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AppUser", policy => policy.RequireRole(ApplicationRoles.All))
    .AddPolicy("AdminOnly", policy => policy.RequireRole(ApplicationRoles.Admin))
    .AddPolicy("ReportsAccess", policy => policy.RequireRole(
        ApplicationRoles.Admin,
        ApplicationRoles.Manager,
        ApplicationRoles.Executive))
    .AddPolicy("VoucherIssueAccess", policy => policy.RequireRole(
        ApplicationRoles.Admin,
        ApplicationRoles.Manager,
        ApplicationRoles.Issuer))
    .AddPolicy("VoucherRedeemAccess", policy => policy.RequireRole(
        ApplicationRoles.Admin,
        ApplicationRoles.Manager,
        ApplicationRoles.Redeemer))
    .AddPolicy("RegistrationDisabled", policy => policy.RequireAssertion(_ => false));

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

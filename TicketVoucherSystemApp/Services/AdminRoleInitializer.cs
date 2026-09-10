using Microsoft.AspNetCore.Identity;

namespace TicketVoucherSystemApp.Services;

public sealed class AdminRoleInitializer(
    IServiceProvider services,
    IConfiguration configuration,
    ILogger<AdminRoleInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        var email = configuration["Admin:Email"];
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogInformation("Admin:Email is not configured; no user was promoted to Admin.");
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(email);
        var bootstrapPassword = configuration["Admin:BootstrapPassword"];
        if (user is null && !string.IsNullOrWhiteSpace(bootstrapPassword))
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, bootstrapPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"The bootstrap admin could not be created: {errors}");
            }
        }

        if (user is not null && !await userManager.IsInRoleAsync(user, "Admin"))
        {
            await userManager.AddToRoleAsync(user, "Admin");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

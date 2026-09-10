using Microsoft.AspNetCore.Identity;
using TicketVoucherSystemApp.Authorization;

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

        foreach (var role in ApplicationRoles.All)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Role '{role}' could not be created: {Errors(result)}");
            }
        }

        var email = configuration["Admin:Email"]?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning(
                "Admin:Email is not configured. Set it to the exact email address used to sign in, restart, then sign out and back in.");
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

            var created = await userManager.CreateAsync(user, bootstrapPassword);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException($"The bootstrap admin could not be created: {Errors(created)}");
            }
        }

        if (user is null)
        {
            logger.LogWarning("Admin user {AdminEmail} was not found. Register that exact email or configure Admin:BootstrapPassword.", email);
            return;
        }

        if (!await userManager.IsInRoleAsync(user, ApplicationRoles.Admin))
        {
            var promoted = await userManager.AddToRoleAsync(user, ApplicationRoles.Admin);
            if (!promoted.Succeeded)
            {
                throw new InvalidOperationException($"The admin role could not be assigned: {Errors(promoted)}");
            }

            await userManager.UpdateSecurityStampAsync(user);
        }

        logger.LogInformation("Admin access is configured for {AdminEmail}. A fresh sign-in is required after a role change.", email);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static string Errors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => error.Description));
}

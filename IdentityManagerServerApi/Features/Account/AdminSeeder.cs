using IdentityManagerServerApi.Data;
using Microsoft.AspNetCore.Identity;

namespace IdentityManagerServerApi.Features.Account;

public static class AdminSeeder
{
    public const string AdminRole = "Admin";
    public const string UserRole = "User";

    /// <summary>
    /// Ensures the Admin and User roles exist and, when <c>Seed:AdminEmail</c> and <c>Seed:AdminPassword</c> are
    /// configured, that an account with that email exists in the Admin role. Idempotent; an existing account's
    /// password is left alone.
    /// </summary>
    public static async Task SeedAdminAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        IServiceProvider services = scope.ServiceProvider;

        RoleManager<IdentityRole> roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (string role in new[] { AdminRole, UserRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
                Throw(await roleManager.CreateAsync(new IdentityRole { Name = role }), $"create role {role}");
        }

        IConfiguration configuration = services.GetRequiredService<IConfiguration>();
        string? email = configuration["Seed:AdminEmail"];
        string? password = configuration["Seed:AdminPassword"];
        ILogger logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Seed:AdminEmail/Seed:AdminPassword not configured; no admin account seeded.");
            return;
        }

        UserManager<ApplicationUser> userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        ApplicationUser? admin = await userManager.FindByEmailAsync(email);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Name = configuration["Seed:AdminName"] ?? "Administrator",
                Email = email,
                UserName = email
            };
            Throw(await userManager.CreateAsync(admin, password), "create the seeded admin");
            logger.LogInformation("Seeded admin account {Email}.", email);
        }

        if (!await userManager.IsInRoleAsync(admin, AdminRole))
            Throw(await userManager.AddToRoleAsync(admin, AdminRole), "add the seeded admin to the Admin role");
    }

    private static void Throw(IdentityResult result, string action)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Failed to {action}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }
}

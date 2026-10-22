using System.Net;
using System.Net.Http.Json;
using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Features.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SharedClassLibrary.DTOs;

namespace AuthorBooksBlazorCore.Test;

[TestFixture]
[NonParallelizable]
public class AdminSeederTests
{
    private const string AdminEmail = "admin@test.local";
    private const string AdminPassword = "Adm1nPassword";

    private CustomWebApplicationFactory<Program> _factory = null!;

    [TearDown]
    public void TearDown() => _factory.Dispose();

    // Creating a client builds the host and the (empty) test database; the seeder itself is skipped under
    // IntegrationTesting, so each test invokes it explicitly.
    private HttpClient Start(bool configured, string password = AdminPassword)
    {
        _factory = new CustomWebApplicationFactory<Program>();

        if (configured)
        {
            _factory.Settings["Seed:AdminEmail"] = AdminEmail;
            _factory.Settings["Seed:AdminPassword"] = password;
        }

        return _factory.CreateAnonymousClient();
    }

    private async Task<(ApplicationUser? User, IList<string> Roles)> FindAdmin()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        ApplicationUser? user = await users.FindByEmailAsync(AdminEmail);
        return (user, user is null ? [] : await users.GetRolesAsync(user));
    }

    [Test]
    public async Task Seed_WhenConfigured_CreatesAdminInAdminRole_WhoCanLogIn()
    {
        using HttpClient client = Start(configured: true);

        await AdminSeeder.SeedAdminAsync(_factory.Services);

        (ApplicationUser? user, IList<string> roles) = await FindAdmin();
        Assert.That(user, Is.Not.Null);
        Assert.That(roles, Is.EqualTo(new[] { "Admin" }));

        HttpResponseMessage login = await client.PostAsJsonAsync(
            "/api/Account/login", new LoginDTO { Email = AdminEmail, Password = AdminPassword });
        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Seed_RunTwice_IsIdempotent_AndKeepsTheExistingPassword()
    {
        using HttpClient client = Start(configured: true);
        await AdminSeeder.SeedAdminAsync(_factory.Services);

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            UserManager<ApplicationUser> users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            ApplicationUser admin = (await users.FindByEmailAsync(AdminEmail))!;
            string token = await users.GeneratePasswordResetTokenAsync(admin);
            Assert.That((await users.ResetPasswordAsync(admin, token, "Ch4ngedPassword")).Succeeded, Is.True);
        }

        await Assert.ThatAsync(() => AdminSeeder.SeedAdminAsync(_factory.Services), Throws.Nothing);

        using IServiceScope check = _factory.Services.CreateScope();
        UserManager<ApplicationUser> checkUsers = check.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.That(checkUsers.Users.Count(u => u.Email == AdminEmail), Is.EqualTo(1));

        HttpResponseMessage login = await client.PostAsJsonAsync(
            "/api/Account/login", new LoginDTO { Email = AdminEmail, Password = "Ch4ngedPassword" });
        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.OK), "seeding again must not reset the password");
    }

    [Test]
    public async Task Seed_ExistingUserWithConfiguredEmail_IsPromotedToAdmin()
    {
        using HttpClient client = Start(configured: true);
        await client.PostAsJsonAsync("/api/Account/register", new UserDTO
        {
            Name = "Pre-existing",
            Email = AdminEmail,
            Password = AdminPassword,
            ConfirmPassword = AdminPassword
        });

        await AdminSeeder.SeedAdminAsync(_factory.Services);

        (_, IList<string> roles) = await FindAdmin();
        Assert.That(roles, Is.EquivalentTo(new[] { "User", "Admin" }));
    }

    [Test]
    public async Task Seed_WhenNotConfigured_CreatesRolesButNoAdminAccount()
    {
        using HttpClient client = Start(configured: false);

        await AdminSeeder.SeedAdminAsync(_factory.Services);

        (ApplicationUser? user, _) = await FindAdmin();
        Assert.That(user, Is.Null);

        using IServiceScope scope = _factory.Services.CreateScope();
        RoleManager<IdentityRole> roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        Assert.That(await roles.RoleExistsAsync("Admin"), Is.True);
        Assert.That(await roles.RoleExistsAsync("User"), Is.True);
    }

    [Test]
    public async Task Seed_WhenPasswordViolatesIdentityRules_FailsLoudly()
    {
        using HttpClient client = Start(configured: true, password: "weak");

        InvalidOperationException? ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => AdminSeeder.SeedAdminAsync(_factory.Services));
        Assert.That(ex!.Message, Does.Contain("create the seeded admin"));
    }
}

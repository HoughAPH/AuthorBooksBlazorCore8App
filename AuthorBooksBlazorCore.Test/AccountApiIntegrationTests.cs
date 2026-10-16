using IdentityManagerServerApi.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Helpers;
using SharedClassLibrary.Models;
using System.Net;
using System.Net.Http.Json;

namespace AuthorBooksBlazorCore.Test;

[TestFixture]
[NonParallelizable]
public class AccountApiIntegrationTests
{
    private const string Email = "reader@test.local";
    private const string Password = "Passw0rdA";
    private const string NewPassword = "N3wPassword";

    private CustomWebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new CustomWebApplicationFactory<Program>();
        _client = _factory.CreateAnonymousClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private Task<HttpResponseMessage> Register(string email = Email, string password = Password, string? confirm = null) =>
        _client.PostAsJsonAsync("/api/Account/register", new UserDTO
        {
            Name = "Test Reader",
            Email = email,
            Password = password,
            ConfirmPassword = confirm ?? password
        });

    private Task<HttpResponseMessage> Login(string email = Email, string password = Password) =>
        _client.PostAsJsonAsync("/api/Account/login", new LoginDTO { Email = email, Password = password });

    private static async Task<ProblemDetails> ReadProblem(HttpResponseMessage response)
    {
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problem, Is.Not.Null);
        return problem!;
    }

    private async Task<string> GetUserId(string email)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return (await users.FindByEmailAsync(email))!.Id;
    }

    // ---- Register ----

    [Test]
    public async Task Register_NewUser_ReturnsOk_AndAssignsUserRole()
    {
        HttpResponseMessage response = await Register();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Result? body = await response.Content.ReadFromJsonAsync<Result>();
        Assert.That(body, Is.EqualTo(Result.Ok("Account Created")));

        using IServiceScope scope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        ApplicationUser user = (await users.FindByEmailAsync(Email))!;
        Assert.That(await users.GetRolesAsync(user), Is.EqualTo(new[] { "User" }));
    }

    [Test]
    public async Task Register_NeverCreatesAnAdmin_EvenForTheFirstAccount()
    {
        await Register();

        using IServiceScope scope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.That(await users.GetUsersInRoleAsync("Admin"), Is.Empty);
    }

    [Test]
    public async Task Register_DuplicateEmail_ReturnsConflictProblem()
    {
        await Register();

        HttpResponseMessage response = await Register();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        ProblemDetails problem = await ReadProblem(response);
        Assert.That(problem.Detail, Is.EqualTo("User registered already"));
    }

    [Test]
    public async Task Register_PasswordFailingIdentityRules_ReturnsBadRequestProblem()
    {
        HttpResponseMessage response = await Register(password: "alllowercase");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        ProblemDetails problem = await ReadProblem(response);
        Assert.That(problem.Detail, Is.Not.Empty);
    }

    [Test]
    public async Task Register_PasswordsDoNotMatch_ReturnsValidationProblem()
    {
        HttpResponseMessage response = await Register(confirm: "Different1");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.That(problem!.Errors, Does.ContainKey(nameof(UserDTO.ConfirmPassword)));
    }

    // ---- Login ----

    [Test]
    public async Task Login_ValidCredentials_ReturnsTokenWithUserClaims()
    {
        await Register();

        HttpResponseMessage response = await Login();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Result<string>? body = await response.Content.ReadFromJsonAsync<Result<string>>();
        Assert.That(body!.Succeeded, Is.True);
        Assert.That(body.Value, Is.Not.Empty);

        UserSession session = JwtHelper.GetClaimsFromToken(body.Value);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.Email, Is.EqualTo(Email));
            Assert.That(session.Role, Is.EqualTo("User"));
            Assert.That(session.Id, Is.EqualTo(await GetUserId(Email)));
        }
    }

    [Test]
    public async Task Login_WrongPasswordAndUnknownEmail_ReturnTheSameUnauthorizedProblem()
    {
        await Register();

        HttpResponseMessage wrongPassword = await Login(password: "Wrong1Password");
        HttpResponseMessage unknownEmail = await Login(email: "nobody@test.local");

        Assert.That(wrongPassword.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(unknownEmail.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // Identical message, so the endpoint doesn't reveal which emails are registered.
        ProblemDetails first = await ReadProblem(wrongPassword);
        ProblemDetails second = await ReadProblem(unknownEmail);
        Assert.That(first.Detail, Is.EqualTo("Email/Password not valid"));
        Assert.That(second.Detail, Is.EqualTo(first.Detail));
    }

    [Test]
    public async Task Login_AfterRepeatedFailures_LocksTheAccountEvenForTheCorrectPassword()
    {
        await Register();

        // Program.cs: lockout after 5 failed attempts.
        for (int i = 0; i < 5; i++)
            Assert.That((await Login(password: "Wrong1Password")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        HttpResponseMessage response = await Login();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        ProblemDetails problem = await ReadProblem(response);
        Assert.That(problem.Detail, Does.Contain("locked"));
    }

    [Test]
    public async Task Login_SuccessfulLogin_ResetsTheFailureCount()
    {
        await Register();

        for (int i = 0; i < 4; i++)
            await Login(password: "Wrong1Password");
        Assert.That((await Login()).StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Four more failures would lock the account if the earlier four were still counted.
        for (int i = 0; i < 4; i++)
            await Login(password: "Wrong1Password");

        Assert.That((await Login()).StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Login_MissingFields_ReturnsValidationProblem()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/Account/login", new { });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    // ---- Me / Logout ----

    [Test]
    public async Task Me_WhenAnonymous_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/Account/me");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Me_ForAuthenticatedUser_ReturnsTheirProfileAndRoles()
    {
        // Creating a client resets the database, so create it first and point it at the user afterwards.
        using HttpClient authed = _factory.CreateAuthenticatedClient(TestAuthHandler.UserRole, "pending");
        await Register();
        authed.DefaultRequestHeaders.Remove(TestAuthHandler.UserHeaderName);
        authed.DefaultRequestHeaders.Add(TestAuthHandler.UserHeaderName, await GetUserId(Email));

        HttpResponseMessage response = await authed.GetAsync("/api/Account/me");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        CurrentUserDto? me = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(me!.IsAuthenticated, Is.True);
            Assert.That(me.Email, Is.EqualTo(Email));
            Assert.That(me.Name, Is.EqualTo("Test Reader"));
            Assert.That(me.Roles, Is.EqualTo(new[] { "User" }));
        }
    }

    [Test]
    public async Task Me_WhenTokenUserNoLongerExists_ReturnsUnauthorized()
    {
        using HttpClient authed = _factory.CreateAuthenticatedClient(TestAuthHandler.UserRole, "deleted-user-id");

        HttpResponseMessage response = await authed.GetAsync("/api/Account/me");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Logout_RequiresAuthentication_AndSucceedsWhenSignedIn()
    {
        Assert.That((await _client.PostAsync("/api/Account/logout", null)).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        using HttpClient authed = _factory.CreateAuthenticatedClient(TestAuthHandler.UserRole);
        HttpResponseMessage response = await authed.PostAsync("/api/Account/logout", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await response.Content.ReadFromJsonAsync<Result>())!.Succeeded, Is.True);
    }

    // ---- Forgot / reset password ----

    [Test]
    public async Task ForgotPassword_KnownAndUnknownEmail_GiveIdenticalResponses_AndNeverReturnTheToken()
    {
        await Register();

        HttpResponseMessage known = await _client.PostAsJsonAsync("/api/Account/forgot-password", new ForgotPasswordDTO { Email = Email });
        HttpResponseMessage unknown = await _client.PostAsJsonAsync("/api/Account/forgot-password", new ForgotPasswordDTO { Email = "nobody@test.local" });

        Assert.That(known.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(unknown.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        string knownBody = await known.Content.ReadAsStringAsync();
        Assert.That(knownBody, Is.EqualTo(await unknown.Content.ReadAsStringAsync()));

        // The token goes to the sender only; it is never in the HTTP response.
        (string sentTo, string token) = _factory.ResetTokens.Sent.Single();
        Assert.That(sentTo, Is.EqualTo(Email));
        Assert.That(token, Is.Not.Empty);
        Assert.That(knownBody, Does.Not.Contain(token));
    }

    [Test]
    public async Task ResetPassword_WithTheSentToken_ChangesThePassword()
    {
        await Register();
        await _client.PostAsJsonAsync("/api/Account/forgot-password", new ForgotPasswordDTO { Email = Email });
        string token = _factory.ResetTokens.Sent.Single().Token;

        HttpResponseMessage reset = await _client.PostAsJsonAsync("/api/Account/reset-password", new ResetPasswordDTO
        {
            Email = Email,
            Token = token,
            Password = NewPassword,
            ConfirmPassword = NewPassword
        });

        Assert.That(reset.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await Login(password: Password)).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await Login(password: NewPassword)).StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task ResetPassword_WithUrlEscapedTokenWhosePlusBecameSpaces_StillWorks()
    {
        await Register();
        await _client.PostAsJsonAsync("/api/Account/forgot-password", new ForgotPasswordDTO { Email = Email });
        string token = _factory.ResetTokens.Sent.Single().Token;

        // What the reset page's query string can deliver: escaped, with '+' decoded to ' '.
        string mangled = Uri.EscapeDataString(token).Replace("%2B", " ");

        HttpResponseMessage reset = await _client.PostAsJsonAsync("/api/Account/reset-password", new ResetPasswordDTO
        {
            Email = Email,
            Token = mangled,
            Password = NewPassword,
            ConfirmPassword = NewPassword
        });

        Assert.That(reset.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task ResetPassword_WithInvalidToken_ReturnsBadRequestProblem_AndKeepsOldPassword()
    {
        await Register();

        HttpResponseMessage reset = await _client.PostAsJsonAsync("/api/Account/reset-password", new ResetPasswordDTO
        {
            Email = Email,
            Token = "not-a-real-token",
            Password = NewPassword,
            ConfirmPassword = NewPassword
        });

        Assert.That(reset.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await ReadProblem(reset);
        Assert.That((await Login()).StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task ResetPassword_WithoutToken_ReturnsBadRequestProblem()
    {
        await Register();

        HttpResponseMessage reset = await _client.PostAsJsonAsync("/api/Account/reset-password", new ResetPasswordDTO
        {
            Email = Email,
            Password = NewPassword,
            ConfirmPassword = NewPassword
        });

        Assert.That(reset.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await ReadProblem(reset)).Detail, Is.EqualTo("Reset token is empty"));
    }

    [Test]
    public async Task ResetPassword_ForUnknownUser_ReturnsNotFoundProblem()
    {
        HttpResponseMessage reset = await _client.PostAsJsonAsync("/api/Account/reset-password", new ResetPasswordDTO
        {
            Email = "nobody@test.local",
            Token = "whatever",
            Password = NewPassword,
            ConfirmPassword = NewPassword
        });

        Assert.That(reset.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await ReadProblem(reset);
    }

    // ---- Rate limiting ----

    [Test]
    public async Task AnonymousAccountActions_AreRateLimited()
    {
        // Matches the default of 20 writes per minute; anonymous callers share one partition in the test server.
        for (int i = 0; i < 20; i++)
            Assert.That((await Login(email: "nobody@test.local")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        HttpResponseMessage limited = await Login(email: "nobody@test.local");

        Assert.That(limited.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
    }
}

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace AuthorBooksBlazorCore.Test;

[TestFixture]
[NonParallelizable]
public class RateLimitingIntegrationTests
{
    // Matches the default of 20 writes per minute in RateLimitPolicies.
    private const int WritesPerMinute = 20;

    // A delete of an unknown id is the cheapest write: it is rate limited like any other but touches no data.
    private const string CheapWrite = "/api/Author/DeleteAuthor/999";

    private CustomWebApplicationFactory<Program> _factory = null!;

    [SetUp]
    public void Setup() => _factory = new CustomWebApplicationFactory<Program>();

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task Writes_BeyondTheLimit_Return429WithProblemDetailsAndRetryAfter()
    {
        using HttpClient admin = _factory.CreateAuthenticatedClient(TestAuthHandler.AdminRole, "user-a");

        for (int i = 0; i < WritesPerMinute; i++)
        {
            HttpResponseMessage allowed = await admin.DeleteAsync(CheapWrite);
            Assert.That(allowed.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"request {i + 1} should not be limited");
        }

        HttpResponseMessage limited = await admin.DeleteAsync(CheapWrite);

        Assert.That(limited.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
        Assert.That(limited.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        Assert.That(limited.Headers.RetryAfter?.Delta?.TotalSeconds, Is.GreaterThan(0));

        ProblemDetails? problem = await limited.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problem, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem!.Status, Is.EqualTo(429));
            Assert.That(problem.Detail, Does.StartWith("Too many requests."));
        }
    }

    [Test]
    public async Task Limit_IsPerUser_OneUserExhaustingItDoesNotBlockAnother()
    {
        using HttpClient userA = _factory.CreateAuthenticatedClient(TestAuthHandler.AdminRole, "user-a");
        using HttpClient userB = _factory.CreateAuthenticatedClient(TestAuthHandler.AdminRole, "user-b");

        for (int i = 0; i < WritesPerMinute; i++)
            await userA.DeleteAsync(CheapWrite);

        Assert.That((await userA.DeleteAsync(CheapWrite)).StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
        Assert.That((await userB.DeleteAsync(CheapWrite)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Reads_AreNotRateLimited()
    {
        using HttpClient user = _factory.CreateAuthenticatedClient(TestAuthHandler.UserRole, "user-a");

        for (int i = 0; i < WritesPerMinute + 10; i++)
        {
            HttpResponseMessage response = await user.GetAsync("/api/Author/GetAuthors");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"read {i + 1} should not be limited");
        }
    }

    [Test]
    public async Task ExhaustedWriteQuota_DoesNotAffectReadsForTheSameUser()
    {
        using HttpClient admin = _factory.CreateAuthenticatedClient(TestAuthHandler.AdminRole, "user-a");

        for (int i = 0; i <= WritesPerMinute; i++)
            await admin.DeleteAsync(CheapWrite);

        Assert.That((await admin.GetAsync("/api/Author/GetAuthors")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}

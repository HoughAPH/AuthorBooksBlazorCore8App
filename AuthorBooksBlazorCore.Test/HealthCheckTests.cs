using System.Net;

namespace AuthorBooksBlazorCore.Test;

[TestFixture]
[NonParallelizable]
public class HealthCheckTests
{
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

    [Test]
    public async Task Health_ChecksTheDatabaseAndIsAnonymous()
    {
        HttpResponseMessage response = await _client.GetAsync("/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("Healthy"));
    }

    [Test]
    public async Task HealthLive_IsHealthyWithoutTouchingTheDatabase()
    {
        HttpResponseMessage response = await _client.GetAsync("/health/live");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}

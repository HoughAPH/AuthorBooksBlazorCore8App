using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Domain;
using IdentityManagerServerApi.Repositories;
using Microsoft.Extensions.DependencyInjection;
using SharedClassLibrary.DTOs;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AuthorBooksBlazorCore.Test;

[TestFixture]
public class AuthorVersionTests
{
    [Test]
    public void EveryMutationReplacesTheVersion()
    {
        Author author = Author.Create("Original");
        Author other = Author.Create("Other");
        Guid version = author.Version;

        author.Rename("Renamed");
        Assert.That(author.Version, Is.Not.EqualTo(version));

        version = author.Version;
        author.AddBook("A");
        Assert.That(author.Version, Is.Not.EqualTo(version));

        // Unsaved books all have id 0, which is enough to address the single book here.
        version = author.Version;
        author.UpdateBook(0, "B");
        Assert.That(author.Version, Is.Not.EqualTo(version));

        version = author.Version;
        Guid otherVersion = other.Version;
        author.TransferBook(0, other, "B");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(author.Version, Is.Not.EqualTo(version));
            Assert.That(other.Version, Is.Not.EqualTo(otherVersion));
        }

        version = other.Version;
        other.RemoveBook(0);
        Assert.That(other.Version, Is.Not.EqualTo(version));
    }

    [Test]
    public void EnsureVersion_RejectsAStaleVersion_AndAllowsMatchOrNull()
    {
        Author author = Author.Create("Author");
        Guid stale = author.Version;
        author.Rename("Changed");

        DomainException ex = Assert.Throws<DomainException>(() => author.EnsureVersion(stale))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Kind, Is.EqualTo(DomainErrorKind.Conflict));
            Assert.DoesNotThrow(() => author.EnsureVersion(author.Version));
            Assert.DoesNotThrow(() => author.EnsureVersion(null));
        }
    }
}

[TestFixture]
[NonParallelizable]
public class AuthorConcurrencyIntegrationTests
{
    private CustomWebApplicationFactory<Program> _factory = null!;
    private HttpClient _adminClient = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new CustomWebApplicationFactory<Program>();
        _adminClient = _factory.CreateAuthenticatedClient(TestAuthHandler.AdminRole);
    }

    [TearDown]
    public void TearDown()
    {
        _adminClient.Dispose();
        _factory.Dispose();
    }

    private async Task<AuthorDTO> GetAuthor(int id) =>
        (await _adminClient.GetFromJsonAsync<AuthorDTO>($"/api/Author/GetAuthor/{id}"))!;

    [Test]
    public async Task UpdateAuthor_WithCurrentVersion_Succeeds_ThenTheOldVersionIsStale()
    {
        AuthorDTO loadedByAlice = await GetAuthor(3);
        AuthorDTO loadedByBob = await GetAuthor(3);

        loadedByAlice.Name = "Alice Edit";
        HttpResponseMessage first = await _adminClient.PutAsJsonAsync("/api/Author/UpdateAuthor/3", loadedByAlice);

        loadedByBob.Name = "Bob Edit";
        HttpResponseMessage second = await _adminClient.PutAsJsonAsync("/api/Author/UpdateAuthor/3", loadedByBob);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(await ProblemDetail(second), Does.Contain("changed by someone else"));
            Assert.That((await GetAuthor(3)).Name, Is.EqualTo("Alice Edit"));
        }
    }

    [Test]
    public async Task UpdateAuthor_AfterTheAuthorsBooksChange_IsAlsoStale()
    {
        AuthorDTO loaded = await GetAuthor(1);

        HttpResponseMessage add = await _adminClient.PostAsJsonAsync(
            "/api/Book/AddBook", new BookDTO { Title = "Implementing DDD", AuthorId = 1 });

        loaded.Name = "Eric J. Evans";
        HttpResponseMessage update = await _adminClient.PutAsJsonAsync("/api/Author/UpdateAuthor/1", loaded);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(add.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(update.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        }
    }

    [Test]
    public async Task UpdateAuthor_WithoutAVersion_StillWorks()
    {
        HttpResponseMessage response = await _adminClient.PutAsJsonAsync(
            "/api/Author/UpdateAuthor/3", new AuthorDTO { Name = "No Version Sent" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>Two requests that both loaded the author before either saved: the second write must lose.</summary>
    [Test]
    public async Task SaveChanges_WhenTheAuthorChangedAfterItWasLoaded_ThrowsConflict()
    {
        using IServiceScope scopeA = _factory.Services.CreateScope();
        using IServiceScope scopeB = _factory.Services.CreateScope();
        AuthorRepository repoA = new(scopeA.ServiceProvider.GetRequiredService<AppDbContext>());
        AuthorRepository repoB = new(scopeB.ServiceProvider.GetRequiredService<AppDbContext>());

        Author a = (await repoA.GetById(3))!;
        Author b = (await repoB.GetById(3))!;

        a.Rename("First writer");
        b.Rename("Second writer");

        await repoA.SaveChanges();
        DomainException ex = (await Assert.ThrowsAsync<DomainException>(() => repoB.SaveChanges()))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Kind, Is.EqualTo(DomainErrorKind.Conflict));
            Assert.That((await GetAuthor(3)).Name, Is.EqualTo("First writer"));
        }
    }

    private static async Task<string> ProblemDetail(HttpResponseMessage response)
    {
        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("detail").GetString()!;
    }
}

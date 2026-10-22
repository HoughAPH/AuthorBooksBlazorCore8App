using System.Net;
using System.Net.Http.Json;
using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Domain;
using Microsoft.Extensions.DependencyInjection;
using SharedClassLibrary.DTOs;

namespace AuthorBooksBlazorCore.Test;

[TestFixture]
[NonParallelizable]
public class PagingIntegrationTests
{
    private CustomWebApplicationFactory<Program> _factory = null!;
    private HttpClient _anonymousClient = null!;
    private HttpClient _userClient = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new CustomWebApplicationFactory<Program>();
        _anonymousClient = _factory.CreateAnonymousClient();
        _userClient = _factory.CreateAuthenticatedClient(TestAuthHandler.UserRole);

        // Seeded: Eric Evans (Domain Driven Design), Robert Martin (Clean Code), Removable Author. Add 9 more authors.
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        for (int i = 1; i <= 9; i++)
        {
            Author author = Author.Create($"Zed Author {i:00}");
            author.AddBook($"Zed Book {i:00}");
            db.Authors.Add(author);
        }

        db.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _anonymousClient.Dispose();
        _userClient.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task GetAuthorsPage_WhenAnonymous_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _anonymousClient.GetAsync("/api/Author/GetAuthorsPage");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetAuthorsPage_ReturnsOnePageOrderedByNameWithTotals()
    {
        PagedResult<AuthorDTO> page = (await _userClient.GetFromJsonAsync<PagedResult<AuthorDTO>>(
            "/api/Author/GetAuthorsPage?pageNumber=2&pageSize=5"))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.TotalCount, Is.EqualTo(12));
            Assert.That(page.TotalPages, Is.EqualTo(3));
            Assert.That(page.PageNumber, Is.EqualTo(2));
            Assert.That(page.Items, Has.Count.EqualTo(5));
            Assert.That(page.Items.Select(a => a.Name), Is.Ordered.Ascending);
            Assert.That(page.Items.All(a => a.Version is not null), Is.True);
        }
    }

    [Test]
    public async Task GetAuthorsPage_FiltersBySearchCaseInsensitively()
    {
        PagedResult<AuthorDTO> page = (await _userClient.GetFromJsonAsync<PagedResult<AuthorDTO>>(
            "/api/Author/GetAuthorsPage?search=EVANS"))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.Items.Select(a => a.Name), Is.EqualTo(new[] { "Eric Evans" }));
            Assert.That(page.TotalCount, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task GetAuthorsPage_CapsPageSizeAndClampsPageNumber()
    {
        PagedResult<AuthorDTO> page = (await _userClient.GetFromJsonAsync<PagedResult<AuthorDTO>>(
            "/api/Author/GetAuthorsPage?pageNumber=-4&pageSize=100000"))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.PageNumber, Is.EqualTo(1));
            Assert.That(page.PageSize, Is.EqualTo(100));
            Assert.That(page.Items, Has.Count.EqualTo(12));
        }
    }

    [Test]
    public async Task GetBooksPage_FiltersByAuthorAndSearch()
    {
        PagedResult<BookDTO> byAuthor = (await _userClient.GetFromJsonAsync<PagedResult<BookDTO>>(
            "/api/Book/GetBooksPage?authorId=1"))!;
        PagedResult<BookDTO> bySearch = (await _userClient.GetFromJsonAsync<PagedResult<BookDTO>>(
            "/api/Book/GetBooksPage?search=zed%20book&pageSize=4"))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(byAuthor.Items.Select(b => b.Title), Is.EqualTo(new[] { "Domain Driven Design" }));
            Assert.That(bySearch.TotalCount, Is.EqualTo(9));
            Assert.That(bySearch.Items, Has.Count.EqualTo(4));
            Assert.That(bySearch.Items.Select(b => b.Title), Is.Ordered.Ascending);
        }
    }
}

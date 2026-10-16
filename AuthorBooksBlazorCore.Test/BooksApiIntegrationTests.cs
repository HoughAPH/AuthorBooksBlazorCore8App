using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedClassLibrary.DTOs;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using static SharedClassLibrary.Models.ServiceResponses;

namespace AuthorBooksBlazorCore.Test;

[TestFixture]
public class BooksApiIntegrationTests
{
    private CustomWebApplicationFactory<Program> _factory = null!;
    private HttpClient _anonymousClient = null!;
    private HttpClient _userClient = null!;
    private HttpClient _adminClient = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new CustomWebApplicationFactory<Program>();
        _anonymousClient = _factory.CreateAnonymousClient();
        _userClient = _factory.CreateAuthenticatedClient(TestAuthHandler.UserRole);
        _adminClient = _factory.CreateAuthenticatedClient(TestAuthHandler.AdminRole);
    }

    [TearDown]
    public void TearDown()
    {
        _anonymousClient?.Dispose();
        _userClient?.Dispose();
        _adminClient?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task GetBooks_WhenAnonymous_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _anonymousClient.GetAsync("/api/Book/GetBooks");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetBooks_WhenAuthenticated_ReturnsSeededBooks()
    {
        HttpResponseMessage response = await _userClient.GetAsync("/api/Book/GetBooks");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        List<BookDTO>? books = await response.Content.ReadFromJsonAsync<List<BookDTO>>();
        Assert.That(books, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(books!.Count, Is.EqualTo(2));
            Assert.That(books.Any(x => x.Title == "Domain Driven Design"), Is.True);
            Assert.That(books.Any(x => x.Title == "Clean Code"), Is.True);
        }
    }

    [Test]
    public async Task GetAuthorBooks_WhenAuthenticated_ReturnsFilteredBooks()
    {
        HttpResponseMessage response = await _userClient.GetAsync("/api/Book/GetAuthorBooks/1");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        List<BookDTO>? books = await response.Content.ReadFromJsonAsync<List<BookDTO>>();
        Assert.That(books, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(books!.Count, Is.EqualTo(1));
            Assert.That(books[0].AuthorId, Is.EqualTo(1));
            Assert.That(books[0].Title, Is.EqualTo("Domain Driven Design"));
        }
    }

    [Test]
    public async Task GetBook_WhenBookExists_ReturnsOk()
    {
        HttpResponseMessage response = await _userClient.GetAsync("/api/Book/GetBook/1");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        BookDTO? book = await response.Content.ReadFromJsonAsync<BookDTO>();
        Assert.That(book, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(book!.Id, Is.EqualTo(1));
            Assert.That(book.Title, Is.EqualTo("Domain Driven Design"));
        }
    }

    [Test]
    public async Task GetBook_WhenBookDoesNotExist_ReturnsNotFound()
    {
        HttpResponseMessage response = await _userClient.GetAsync("/api/Book/GetBook/999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AddBook_WhenAuthenticatedButNotAdmin_ReturnsForbidden()
    {
        BookDTO request = new()
        {
            Title = "Refactoring",
            AuthorId = 1
        };

        HttpResponseMessage response = await _userClient.PostAsJsonAsync("/api/Book/AddBook", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task AddBook_WhenAdmin_ReturnsOk_AndPersistsBook()
    {
        BookDTO request = new()
        {
            Title = "Refactoring",
            AuthorId = 1
        };

        HttpResponseMessage response = await _adminClient.PostAsJsonAsync("/api/Book/AddBook", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        GeneralResponse? result = await response.Content.ReadFromJsonAsync<GeneralResponse>();
        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result!.Flag, Is.True);
            Assert.That(result.Message, Is.EqualTo("Book added successfully."));
        }

        List<BookDTO>? books = await _adminClient.GetFromJsonAsync<List<BookDTO>>("/api/Book/GetBooks");
        Assert.That(books, Is.Not.Null);
        Assert.That(books!.Any(x => x.Title == "Refactoring" && x.AuthorId == 1), Is.True);
    }

    [Test]
    public async Task UpdateBook_WhenAuthenticatedButNotAdmin_ReturnsForbidden()
    {
        BookDTO request = new()
        {
            Id = 1,
            Title = "Updated Title",
            AuthorId = 1
        };

        HttpResponseMessage response = await _userClient.PutAsJsonAsync("/api/Book/UpdateBook/1", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task UpdateBook_WhenAdmin_ReturnsOk_AndUpdatesBook()
    {
        BookDTO request = new()
        {
            Id = 1,
            Title = "Updated Title",
            AuthorId = 1
        };

        HttpResponseMessage response = await _adminClient.PutAsJsonAsync("/api/Book/UpdateBook/1", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        GeneralResponse? result = await response.Content.ReadFromJsonAsync<GeneralResponse>();
        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result!.Flag, Is.True);
            Assert.That(result.Message, Is.EqualTo("Book updated successfully."));
        }

        BookDTO? updated = await _adminClient.GetFromJsonAsync<BookDTO>("/api/Book/GetBook/1");
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Title, Is.EqualTo("Updated Title"));
    }

    [Test]
    public async Task AddBook_WhenAuthorDoesNotExist_ReturnsNotFoundProblem()
    {
        HttpResponseMessage response = await _adminClient.PostAsJsonAsync(
            "/api/Book/AddBook", new BookDTO { Title = "Orphan", AuthorId = 999 });

        await AssertProblem(response, HttpStatusCode.NotFound, "Author not found.");
    }

    [Test]
    public async Task AddBook_WithDuplicateTitleForSameAuthor_ReturnsConflictProblem()
    {
        HttpResponseMessage response = await _adminClient.PostAsJsonAsync(
            "/api/Book/AddBook", new BookDTO { Title = "domain driven design", AuthorId = 1 });

        await AssertProblem(response, HttpStatusCode.Conflict, "This author already has a book with that title.");
    }

    [Test]
    public async Task AddBook_WithTooLongTitle_ReturnsBadRequestProblem()
    {
        HttpResponseMessage response = await _adminClient.PostAsJsonAsync(
            "/api/Book/AddBook", new BookDTO { Title = new string('b', 201), AuthorId = 1 });

        await AssertProblem(response, HttpStatusCode.BadRequest, "Book title cannot exceed 200 characters.");
    }

    [Test]
    public async Task UpdateBook_WhenBookDoesNotExist_ReturnsNotFoundProblem()
    {
        HttpResponseMessage response = await _adminClient.PutAsJsonAsync(
            "/api/Book/UpdateBook/999", new BookDTO { Id = 999, Title = "Ghost", AuthorId = 1 });

        await AssertProblem(response, HttpStatusCode.NotFound, "Book not found.");
    }

    [Test]
    public async Task UpdateBook_WhenMovedToAnotherAuthor_KeepsBookId()
    {
        HttpResponseMessage response = await _adminClient.PutAsJsonAsync(
            "/api/Book/UpdateBook/1", new BookDTO { Id = 1, Title = "Domain Driven Design", AuthorId = 3 });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        BookDTO? moved = await _adminClient.GetFromJsonAsync<BookDTO>("/api/Book/GetBook/1");
        Assert.That(moved, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(moved!.AuthorId, Is.EqualTo(3));
            Assert.That(moved.AuthorName, Is.EqualTo("Removable Author"));
        }
    }

    [Test]
    public async Task DeleteBook_WhenBookDoesNotExist_ReturnsNotFoundProblem()
    {
        HttpResponseMessage response = await _adminClient.DeleteAsync("/api/Book/DeleteBook/999");

        await AssertProblem(response, HttpStatusCode.NotFound, "Book not found.");
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string detail)
    {
        Assert.That(response.StatusCode, Is.EqualTo(status));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problem, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem!.Status, Is.EqualTo((int)status));
            Assert.That(problem.Detail, Is.EqualTo(detail));
        }
    }

    [Test]
    public async Task DeleteBook_WhenAuthenticatedButNotAdmin_ReturnsForbidden()
    {
        HttpResponseMessage response = await _userClient.DeleteAsync("/api/Book/DeleteBook/1");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task DeleteBook_WhenAdmin_ReturnsOk_AndDeletesBook()
    {
        HttpResponseMessage response = await _adminClient.DeleteAsync("/api/Book/DeleteBook/1");
        GeneralResponse expected = new(true, "Book deleted successfully.");

        GeneralResponse generalResponse = await response.Content.ReadFromJsonAsync<GeneralResponse>();
        Assert.That(generalResponse, Is.Not.Null);
        Assert.That(generalResponse!.Message, Is.EqualTo(expected.Message));

        HttpResponseMessage getDeleted = await _adminClient.GetAsync("/api/Book/GetBook/1");
        Assert.That(getDeleted.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

}

internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    internal const string SchemeName = "TestScheme";
    internal const string RoleHeaderName = "X-Test-Role";
    internal const string UserHeaderName = "X-Test-User";
    internal const string DefaultUserId = "integration-test-user";
    internal const string AdminRole = "Admin";
    internal const string UserRole = "User";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RoleHeaderName, out Microsoft.Extensions.Primitives.StringValues value))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string role = value.ToString();
        string userId = Request.Headers.TryGetValue(UserHeaderName, out Microsoft.Extensions.Primitives.StringValues user)
            ? user.ToString()
            : DefaultUserId;

        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userId),
            new Claim(ClaimTypes.Email, "integration@test.local"),
            new Claim(ClaimTypes.Role, role)
        ];

        ClaimsIdentity identity = new(claims, SchemeName);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

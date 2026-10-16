using Microsoft.AspNetCore.Mvc;
using SharedClassLibrary.DTOs;
using System.Net;
using System.Net.Http.Json;
using SharedClassLibrary.Models;

namespace AuthorBooksBlazorCore.Test;

[TestFixture]
[NonParallelizable]
public class AuthorsApiIntegrationTests
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
        _anonymousClient.Dispose();
        _userClient.Dispose();
        _adminClient.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task GetAuthors_WhenAnonymous_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _anonymousClient.GetAsync("/api/Author/GetAuthors");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetAuthors_WhenAuthenticated_ReturnsSeededAuthors()
    {
        HttpResponseMessage response = await _userClient.GetAsync("/api/Author/GetAuthors");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        List<AuthorDTO>? authors = await response.Content.ReadFromJsonAsync<List<AuthorDTO>>();
        Assert.That(authors, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(authors!.Count, Is.EqualTo(3));
            Assert.That(authors.Any(x => x.Name == "Eric Evans"), Is.True);
            Assert.That(authors.Any(x => x.Name == "Robert Martin"), Is.True);
        }
    }

    [Test]
    public async Task GetAuthor_WhenAuthorExists_ReturnsOk()
    {
        HttpResponseMessage response = await _userClient.GetAsync("/api/Author/GetAuthor/1");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        AuthorDTO? author = await response.Content.ReadFromJsonAsync<AuthorDTO>();
        Assert.That(author, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(author!.Id, Is.EqualTo(1));
            Assert.That(author.Name, Is.EqualTo("Eric Evans"));
        }
    }

    [Test]
    public async Task GetAuthor_WhenAuthorDoesNotExist_ReturnsNotFound()
    {
        HttpResponseMessage response = await _userClient.GetAsync("/api/Author/GetAuthor/999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AddAuthor_WhenAuthenticatedButNotAdmin_ReturnsForbidden()
    {
        AuthorDTO request = new()
        {
            Name = "Martin Fowler"
        };

        HttpResponseMessage response = await _userClient.PostAsJsonAsync("/api/Author/AddAuthor", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task AddAuthor_WhenAdmin_ReturnsOk_AndPersistsAuthor()
    {
        AuthorDTO request = new()
        {
            Name = "Martin Fowler"
        };

        HttpResponseMessage response = await _adminClient.PostAsJsonAsync("/api/Author/AddAuthor", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        Result? result = await response.Content.ReadFromJsonAsync<Result>();
        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result!.Succeeded, Is.True);
            Assert.That(result.Message, Is.EqualTo("Author added successfully."));
        }

        List<AuthorDTO>? authors = await _adminClient.GetFromJsonAsync<List<AuthorDTO>>("/api/Author/GetAuthors");
        Assert.That(authors, Is.Not.Null);
        Assert.That(authors!.Any(x => x.Name == "Martin Fowler"), Is.True);
    }

    [Test]
    public async Task UpdateAuthor_WhenAuthenticatedButNotAdmin_ReturnsForbidden()
    {
        AuthorDTO request = new()
        {
            Id = 1,
            Name = "Updated Author"
        };

        HttpResponseMessage response = await _userClient.PutAsJsonAsync("/api/Author/UpdateAuthor/1", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task UpdateAuthor_WhenAdmin_ReturnsOk_AndUpdatesAuthor()
    {
        AuthorDTO request = new()
        {
            Id = 1,
            Name = "Updated Author"
        };

        HttpResponseMessage response = await _adminClient.PutAsJsonAsync("/api/Author/UpdateAuthor/1", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        Result? result = await response.Content.ReadFromJsonAsync<Result>();
        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result!.Succeeded, Is.True);
            Assert.That(result.Message, Is.EqualTo("Author updated successfully."));
        }

        AuthorDTO? updated = await _adminClient.GetFromJsonAsync<AuthorDTO>("/api/Author/GetAuthor/1");
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Name, Is.EqualTo("Updated Author"));
    }

    [Test]
    public async Task DeleteAuthor_WhenAuthenticatedButNotAdmin_ReturnsForbidden()
    {
        HttpResponseMessage response = await _userClient.DeleteAsync("/api/Author/DeleteAuthor/3");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task DeleteAuthor_WhenAdmin_AndAuthorHasBooks_ReturnsConflictProblem()
    {
        HttpResponseMessage response = await _adminClient.DeleteAsync("/api/Author/DeleteAuthor/1");

        await AssertProblem(response, HttpStatusCode.Conflict, "Author cannot be deleted because the author has books.");
    }

    [Test]
    public async Task DeleteAuthor_WhenAuthorDoesNotExist_ReturnsNotFoundProblem()
    {
        HttpResponseMessage response = await _adminClient.DeleteAsync("/api/Author/DeleteAuthor/999");

        await AssertProblem(response, HttpStatusCode.NotFound, "Author not found.");
    }

    [Test]
    public async Task AddAuthor_WithTooLongName_ReturnsBadRequestProblem()
    {
        HttpResponseMessage response =
            await _adminClient.PostAsJsonAsync("/api/Author/AddAuthor", new AuthorDTO { Name = new string('a', 201) });

        await AssertProblem(response, HttpStatusCode.BadRequest, "Author name cannot exceed 200 characters.");
    }

    [Test]
    public async Task AddAuthor_WithWhitespaceName_ReturnsValidationProblem()
    {
        HttpResponseMessage response =
            await _adminClient.PostAsJsonAsync("/api/Author/AddAuthor", new AuthorDTO { Name = "   " });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
    }

    [Test]
    public async Task UpdateAuthor_WhenAuthorDoesNotExist_ReturnsNotFoundProblem()
    {
        HttpResponseMessage response = await _adminClient.PutAsJsonAsync(
            "/api/Author/UpdateAuthor/999", new AuthorDTO { Id = 999, Name = "Nobody" });

        await AssertProblem(response, HttpStatusCode.NotFound, "Author not found.");
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
    public async Task DeleteAuthor_WhenAdmin_AndAuthorHasNoBooks_DeletesAuthor()
    {
        HttpResponseMessage response = await _adminClient.DeleteAsync("/api/Author/DeleteAuthor/3");
        Result expected = Result.Ok("Author deleted successfully.");

        Result? generalResponse = await response.Content.ReadFromJsonAsync<Result>();

        Assert.That(generalResponse, Is.Not.Null);
        Assert.That(generalResponse!.Message, Is.EqualTo(expected.Message));

        HttpResponseMessage getDeleted = await _adminClient.GetAsync("/api/Author/GetAuthor/3");
        Assert.That(getDeleted.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}

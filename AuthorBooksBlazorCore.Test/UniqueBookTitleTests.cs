using System.Net;
using System.Text.Json;
using IdentityManagerServerApi.Domain;
using IdentityManagerServerApi.Exceptions;
using IdentityManagerServerApi.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuthorBooksBlazorCore.Test;

[TestFixture]
[NonParallelizable]
public class UniqueBookTitleTests
{
    private CustomWebApplicationFactory<Program> _factory = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new CustomWebApplicationFactory<Program>();
        _factory.CreateAnonymousClient().Dispose(); // builds the host and the seeded test database
    }

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task ConcurrentAddsOfTheSameTitle_SecondSaveIsRejectedAsConflict_AndOnlyOneBookIsStored()
    {
        // Two requests load the same author before either saves, so the in-memory duplicate check passes for both.
        // Adding a book changes the aggregate's version, so the second save is stopped by the concurrency token.
        using IServiceScope scopeA = _factory.Services.CreateScope();
        using IServiceScope scopeB = _factory.Services.CreateScope();
        IAuthorRepository repoA = scopeA.ServiceProvider.GetRequiredService<IAuthorRepository>();
        IAuthorRepository repoB = scopeB.ServiceProvider.GetRequiredService<IAuthorRepository>();

        Author authorA = (await repoA.GetById(1))!;
        Author authorB = (await repoB.GetById(1))!;
        authorA.AddBook("Racing Title");
        authorB.AddBook("Racing Title");

        Assert.That(await repoA.SaveChanges(), Is.GreaterThan(0));

        DomainException? ex = Assert.Throws<DomainException>(() => repoB.SaveChanges().GetAwaiter().GetResult());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex!.Kind, Is.EqualTo(DomainErrorKind.Conflict));
            Assert.That(ex.Message, Does.Contain("changed or deleted by someone else"));
        }

        using IServiceScope check = _factory.Services.CreateScope();
        Author stored = (await check.ServiceProvider.GetRequiredService<IAuthorRepository>().GetById(1))!;
        Assert.That(stored.Books.Count(b => b.Title == "Racing Title"), Is.EqualTo(1));
    }

    /// <summary>
    /// The unique (AuthorId, Title) index is the last line of defence for writes that do not go through the author's
    /// version, so it must still surface as the duplicate-title conflict.
    /// </summary>
    [Test]
    public async Task UniqueIndexViolation_IsReportedAsDuplicateTitleConflict()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IAuthorRepository repo = scope.ServiceProvider.GetRequiredService<IAuthorRepository>();
        IdentityManagerServerApi.Data.AppDbContext db =
            scope.ServiceProvider.GetRequiredService<IdentityManagerServerApi.Data.AppDbContext>();

        Author author = (await repo.GetById(1))!;
        Book first = new("Index Title");
        first.MoveTo(author);
        db.Add(first);
        await repo.SaveChanges();

        Book duplicate = new("Index Title");
        duplicate.MoveTo(author);
        db.Add(duplicate);

        DomainException? ex = Assert.Throws<DomainException>(() => repo.SaveChanges().GetAwaiter().GetResult());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex!.Kind, Is.EqualTo(DomainErrorKind.Conflict));
            Assert.That(ex.Message, Is.EqualTo("This author already has a book with that title."));
        }
    }

    [Test]
    public async Task SameTitleForDifferentAuthors_IsStillAllowed()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IAuthorRepository repo = scope.ServiceProvider.GetRequiredService<IAuthorRepository>();

        // Seed: author 1 has "Domain Driven Design"; author 2 has "Clean Code".
        Author other = (await repo.GetById(2))!;
        other.AddBook("Domain Driven Design");

        Assert.That(await repo.SaveChanges(), Is.GreaterThan(0));
    }

    [TestCase(DomainErrorKind.Conflict, HttpStatusCode.Conflict)]
    [TestCase(DomainErrorKind.NotFound, HttpStatusCode.NotFound)]
    [TestCase(DomainErrorKind.Validation, HttpStatusCode.BadRequest)]
    public async Task GlobalHandler_MapsDomainExceptionToProblemDetailsWithItsMessage(DomainErrorKind kind, HttpStatusCode expected)
    {
        GlobalExceptionsHandler handler = new(NullLogger<GlobalExceptionsHandler>.Instance, new FakeEnvironment("Production"));
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();

        bool handled = await handler.TryHandleAsync(context, new DomainException("Specific rule message.", kind), CancellationToken.None);

        Assert.That(handled, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo((int)expected));
        Assert.That(context.Response.ContentType, Does.StartWith("application/problem+json"));

        context.Response.Body.Position = 0;
        ProblemDetails? problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.That(problem!.Detail, Is.EqualTo("Specific rule message."));
        Assert.That(problem.Status, Is.EqualTo((int)expected));
    }

    [Test]
    public async Task GlobalHandler_StillHidesDetailsOfUnexpectedExceptionsInProduction()
    {
        GlobalExceptionsHandler handler = new(NullLogger<GlobalExceptionsHandler>.Instance, new FakeEnvironment("Production"));
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();

        await handler.TryHandleAsync(context, new Exception("secret internals"), CancellationToken.None);

        Assert.That(context.Response.StatusCode, Is.EqualTo(500));
        context.Response.Body.Position = 0;
        Assert.That(await new StreamReader(context.Response.Body).ReadToEndAsync(), Does.Not.Contain("secret internals"));
    }

    private sealed class FakeEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

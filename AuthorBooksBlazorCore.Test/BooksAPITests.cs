using IdentityManagerServerApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using System.Reflection;
using static SharedClassLibrary.Models.ServiceResponses;

namespace AuthorBooksBlazorCore.Test;

[TestFixture]
public class BookControllerTests
{
    private Mock<IBooks> _booksRepo = null!;
    private BookController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _booksRepo = new Mock<IBooks>();
        _controller = new BookController(_booksRepo.Object);
    }

    [Test]
    public void GetBooks_Should_Require_Authorized_User()
    {
        AssertRequiresAuthenticatedUser(nameof(BookController.GetBooks));
    }

    [Test]
    public void GetAuthorBooks_Should_Require_Authorized_User()
    {
        AssertRequiresAuthenticatedUser(nameof(BookController.GetAuthorBooks));
    }

    [Test]
    public void GetBook_Should_Require_Authorized_User()
    {
        AssertRequiresAuthenticatedUser(nameof(BookController.GetBook));
    }

    [Test]
    public void AddBook_Should_Require_Admin_Role()
    {
        AssertRequiresAdminRole(nameof(BookController.AddBook));
    }

    [Test]
    public void UpdateBook_Should_Require_Admin_Role()
    {
        AssertRequiresAdminRole(nameof(BookController.UpdateBook));
    }

    [Test]
    public void DeleteBook_Should_Require_Admin_Role()
    {
        AssertRequiresAdminRole(nameof(BookController.DeleteBook));
    }

    [Test]
    public async Task GetBooks_Should_Return_All_Books()
    {
        List<BookDTO> expected =
        [
            new() { Id = 1, Title = "Book 1", AuthorId = 10, AuthorName = "Author A" },
            new() { Id = 2, Title = "Book 2", AuthorId = 20, AuthorName = "Author B" }
        ];

        _booksRepo.Setup(x => x.GetBooks()).ReturnsAsync(expected);

        ActionResult<IEnumerable<BookDTO>> result = await _controller.GetBooks();

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

        OkObjectResult ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.SameAs(expected));
        _booksRepo.Verify(x => x.GetBooks(), Times.Once);
    }

    [Test]
    public async Task GetAuthorBooks_Should_Return_Books_For_Author()
    {
        const int authorId = 10;

        List<BookDTO> expected =
        [
            new() { Id = 1, Title = "Book 1", AuthorId = authorId, AuthorName = "Author A" }
        ];

        _booksRepo.Setup(x => x.GetAuthorBooks(authorId)).ReturnsAsync(expected);

        ActionResult<IEnumerable<BookDTO>> result = await _controller.GetAuthorBooks(authorId);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

        OkObjectResult ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.SameAs(expected));
        _booksRepo.Verify(x => x.GetAuthorBooks(authorId), Times.Once);
    }

    [Test]
    public async Task GetBook_Should_Return_Ok_When_Book_Exists()
    {
        BookDTO expected = new()
        {
            Id = 1,
            Title = "Clean Architecture",
            AuthorId = 2,
            AuthorName = "Robert C. Martin"
        };

        _booksRepo.Setup(x => x.GetBook(1)).ReturnsAsync(expected);

        ActionResult<BookDTO> result = await _controller.GetBook(1);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

        OkObjectResult ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.SameAs(expected));
        _booksRepo.Verify(x => x.GetBook(1), Times.Once);
    }

    [Test]
    public async Task GetBook_Should_Return_NotFound_When_Book_Does_Not_Exist()
    {
        _booksRepo.Setup(x => x.GetBook(99)).ReturnsAsync((BookDTO?)null);

        ActionResult<BookDTO> result = await _controller.GetBook(99);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        _booksRepo.Verify(x => x.GetBook(99), Times.Once);
    }

    [Test]
    public async Task AddBook_Should_Return_Success_Response()
    {
        BookDTO dto = new()
        {
            Title = "New Book",
            AuthorId = 5
        };

        GeneralResponse expected = new(true, "Book added successfully.");

        _booksRepo.Setup(x => x.AddBook(dto)).ReturnsAsync(expected);

        ActionResult<GeneralResponse> result = await _controller.AddBook(dto);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

        OkObjectResult ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.EqualTo(expected));
        _booksRepo.Verify(x => x.AddBook(dto), Times.Once);
    }

    [Test]
    public async Task UpdateBook_Should_Return_Success_Response()
    {
        const int id = 7;

        BookDTO dto = new()
        {
            Id = id,
            Title = "Updated Book",
            AuthorId = 5
        };

        GeneralResponse expected = new(true, "Book updated successfully.");

        _booksRepo.Setup(x => x.UpdateBook(id, dto)).ReturnsAsync(expected);

        ActionResult<GeneralResponse> result = await _controller.UpdateBook(id, dto);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

        OkObjectResult ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.EqualTo(expected));
        _booksRepo.Verify(x => x.UpdateBook(id, dto), Times.Once);
    }

    [Test]
    public async Task DeleteBook_Should_Return_True_When_Delete_Succeeds()
    {
        const int id = 8;
        GeneralResponse expected = new(true, "Book deleted successfully");

        _booksRepo.Setup(x => x.DeleteBook(id)).ReturnsAsync(expected);

        ActionResult<GeneralResponse> result = await _controller.DeleteBook(id);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

        OkObjectResult ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.EqualTo(expected));
        _booksRepo.Verify(x => x.DeleteBook(id), Times.Once);
    }

    private static void AssertRequiresAuthenticatedUser(string actionName)
    {
        AuthorizeAttribute? controllerAttribute =
            typeof(BookController).GetCustomAttribute<AuthorizeAttribute>();

        MethodInfo method = typeof(BookController).GetMethod(actionName)!;
        AllowAnonymousAttribute? allowAnonymous =
            method.GetCustomAttribute<AllowAnonymousAttribute>();

        AuthorizeAttribute[] methodAuthorizeAttributes =
            method.GetCustomAttributes<AuthorizeAttribute>().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(controllerAttribute, Is.Not.Null);
            Assert.That(allowAnonymous, Is.Null);
            Assert.That(methodAuthorizeAttributes.Any(a => !string.IsNullOrWhiteSpace(a.Roles)), Is.False);
        });
    }

    private static void AssertRequiresAdminRole(string actionName)
    {
        AuthorizeAttribute? controllerAttribute =
            typeof(BookController).GetCustomAttribute<AuthorizeAttribute>();

        MethodInfo method = typeof(BookController).GetMethod(actionName)!;
        AuthorizeAttribute? methodAttribute =
            method.GetCustomAttribute<AuthorizeAttribute>();

        AllowAnonymousAttribute? allowAnonymous =
            method.GetCustomAttribute<AllowAnonymousAttribute>();

        Assert.Multiple(() =>
        {
            Assert.That(controllerAttribute, Is.Not.Null);
            Assert.That(allowAnonymous, Is.Null);
            Assert.That(methodAttribute, Is.Not.Null);
            Assert.That(methodAttribute!.Roles, Is.EqualTo("Admin"));
        });
    }
}
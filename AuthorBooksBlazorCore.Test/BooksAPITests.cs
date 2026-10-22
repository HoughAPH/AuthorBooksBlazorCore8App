using IdentityManagerServerApi.Controllers;
using IdentityManagerServerApi.Features.Books.Commands;
using IdentityManagerServerApi.Features.Books.Queries;
using IdentityManagerServerApi.Features.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Models;
using System.Reflection;

namespace AuthorBooksBlazorCore.Test;

[TestFixture]
public class BookControllerTests
{
    private Mock<IQueryHandler<GetBooksQuery, IEnumerable<BookDTO>>> _getBooks = null!;
    private Mock<IQueryHandler<GetAuthorBooksQuery, IEnumerable<BookDTO>>> _getAuthorBooks = null!;
    private Mock<IQueryHandler<GetBookQuery, BookDTO?>> _getBook = null!;
    private Mock<IQueryHandler<GetBooksPageQuery, PagedResult<BookDTO>>> _getBooksPage = null!;
    private Mock<ICommandHandler<AddBookCommand>> _add = null!;
    private Mock<ICommandHandler<UpdateBookCommand>> _update = null!;
    private Mock<ICommandHandler<DeleteBookCommand>> _delete = null!;
    private BookController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _getBooks = new();
        _getAuthorBooks = new();
        _getBook = new();
        _getBooksPage = new();
        _add = new();
        _update = new();
        _delete = new();

        _controller = new BookController(
            _getBooks.Object, _getAuthorBooks.Object, _getBook.Object, _getBooksPage.Object,
            _add.Object, _update.Object, _delete.Object);
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

        _getBooks.Setup(x => x.Handle(It.IsAny<GetBooksQuery>())).ReturnsAsync(expected);

        ActionResult<IEnumerable<BookDTO>> result = await _controller.GetBooks();

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        Assert.That(((OkObjectResult)result.Result!).Value, Is.SameAs(expected));
    }

    [Test]
    public async Task GetAuthorBooks_Should_Return_Books_For_Author()
    {
        const int authorId = 10;

        List<BookDTO> expected =
        [
            new() { Id = 1, Title = "Book 1", AuthorId = authorId, AuthorName = "Author A" }
        ];

        _getAuthorBooks.Setup(x => x.Handle(new GetAuthorBooksQuery(authorId))).ReturnsAsync(expected);

        ActionResult<IEnumerable<BookDTO>> result = await _controller.GetAuthorBooks(authorId);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        Assert.That(((OkObjectResult)result.Result!).Value, Is.SameAs(expected));
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

        _getBook.Setup(x => x.Handle(new GetBookQuery(1))).ReturnsAsync(expected);

        ActionResult<BookDTO> result = await _controller.GetBook(1);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        Assert.That(((OkObjectResult)result.Result!).Value, Is.SameAs(expected));
    }

    [Test]
    public async Task GetBook_Should_Return_NotFound_When_Book_Does_Not_Exist()
    {
        _getBook.Setup(x => x.Handle(new GetBookQuery(99))).ReturnsAsync((BookDTO?)null);

        ActionResult<BookDTO> result = await _controller.GetBook(99);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task AddBook_Should_Return_Success_Response()
    {
        _add.Setup(x => x.Handle(new AddBookCommand("New Book", 5)))
            .ReturnsAsync(Result.Ok("Book added successfully."));

        ActionResult<Result> result =
            await _controller.AddBook(new BookDTO { Title = "New Book", AuthorId = 5 });

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        Assert.That(((OkObjectResult)result.Result!).Value,
            Is.EqualTo(Result.Ok("Book added successfully.")));
    }

    [Test]
    public async Task AddBook_Should_Return_NotFound_When_Author_Is_Missing()
    {
        _add.Setup(x => x.Handle(It.IsAny<AddBookCommand>()))
            .ReturnsAsync(Result.NotFound("Author not found."));

        ActionResult<Result> result =
            await _controller.AddBook(new BookDTO { Title = "New Book", AuthorId = 5 });

        AssertProblemStatus(result.Result, 404);
    }

    [Test]
    public async Task UpdateBook_Should_Return_Success_Response()
    {
        _update.Setup(x => x.Handle(new UpdateBookCommand(7, "Updated Book", 5)))
            .ReturnsAsync(Result.Ok("Book updated successfully."));

        ActionResult<Result> result =
            await _controller.UpdateBook(7, new BookDTO { Id = 7, Title = "Updated Book", AuthorId = 5 });

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        Assert.That(((OkObjectResult)result.Result!).Value,
            Is.EqualTo(Result.Ok("Book updated successfully.")));
    }

    [Test]
    public async Task UpdateBook_Should_Return_NotFound_When_Book_Is_Missing()
    {
        _update.Setup(x => x.Handle(It.IsAny<UpdateBookCommand>()))
            .ReturnsAsync(Result.NotFound("Book not found."));

        ActionResult<Result> result =
            await _controller.UpdateBook(7, new BookDTO { Title = "x", AuthorId = 5 });

        AssertProblemStatus(result.Result, 404);
    }

    [Test]
    public async Task DeleteBook_Should_Return_True_When_Delete_Succeeds()
    {
        _delete.Setup(x => x.Handle(new DeleteBookCommand(8)))
            .ReturnsAsync(Result.Ok("Book deleted successfully."));

        ActionResult<Result> result = await _controller.DeleteBook(8);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        Assert.That(((OkObjectResult)result.Result!).Value,
            Is.EqualTo(Result.Ok("Book deleted successfully.")));
    }

    [Test]
    public async Task DeleteBook_Should_Return_NotFound_When_Delete_Fails()
    {
        _delete.Setup(x => x.Handle(It.IsAny<DeleteBookCommand>()))
            .ReturnsAsync(Result.NotFound("Book not found."));

        ActionResult<Result> result = await _controller.DeleteBook(8);

        AssertProblemStatus(result.Result, 404);
    }

    private static void AssertProblemStatus(IActionResult? actionResult, int expectedStatus)
    {
        Assert.That(actionResult, Is.InstanceOf<ObjectResult>());

        ObjectResult objectResult = (ObjectResult)actionResult!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(objectResult.StatusCode, Is.EqualTo(expectedStatus));
            Assert.That(objectResult.Value, Is.InstanceOf<ProblemDetails>());
        }
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(controllerAttribute, Is.Not.Null);
            Assert.That(allowAnonymous, Is.Null);
            Assert.That(methodAuthorizeAttributes.Any(a => !string.IsNullOrWhiteSpace(a.Roles)), Is.False);
        }
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(controllerAttribute, Is.Not.Null);
            Assert.That(allowAnonymous, Is.Null);
            Assert.That(methodAttribute, Is.Not.Null);
            Assert.That(methodAttribute!.Roles, Is.EqualTo("Admin"));
        }
    }
}

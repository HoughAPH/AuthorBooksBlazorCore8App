using IdentityManagerServerApi.Controllers;
using IdentityManagerServerApi.Features.Authors.Commands;
using IdentityManagerServerApi.Features.Authors.Queries;
using IdentityManagerServerApi.Features.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SharedClassLibrary.DTOs;
using System.Reflection;
using SharedClassLibrary.Models;

namespace AuthorBooksBlazorCore.Test;

[TestFixture]
public class AuthorControllerTests
{
    private Mock<IQueryHandler<GetAuthorsQuery, List<AuthorDTO>>> _getAuthors = null!;
    private Mock<IQueryHandler<GetAuthorQuery, AuthorDTO?>> _getAuthor = null!;
    private Mock<ICommandHandler<CreateAuthorCommand>> _create = null!;
    private Mock<ICommandHandler<RenameAuthorCommand>> _rename = null!;
    private Mock<ICommandHandler<DeleteAuthorCommand>> _delete = null!;
    private AuthorController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _getAuthors = new();
        _getAuthor = new();
        _create = new();
        _rename = new();
        _delete = new();

        _controller = new AuthorController(
            _getAuthors.Object, _getAuthor.Object, _create.Object, _rename.Object, _delete.Object);
    }

    [Test]
    public void GetAuthors_Should_Require_Authorized_User()
    {
        AssertRequiresAuthenticatedUser(nameof(AuthorController.GetAuthors));
    }

    [Test]
    public void GetAuthor_Should_Require_Authorized_User()
    {
        AssertRequiresAuthenticatedUser(nameof(AuthorController.GetAuthor));
    }

    [Test]
    public void AddAuthor_Should_Require_Admin_Role()
    {
        AssertRequiresAdminRole(nameof(AuthorController.AddAuthor));
    }

    [Test]
    public void UpdateAuthor_Should_Require_Admin_Role()
    {
        AssertRequiresAdminRole(nameof(AuthorController.UpdateAuthor));
    }

    [Test]
    public void DeleteAuthor_Should_Require_Admin_Role()
    {
        AssertRequiresAdminRole(nameof(AuthorController.DeleteAuthor));
    }

    [Test]
    public async Task GetAuthors_Should_Return_All_Authors()
    {
        List<AuthorDTO> expected =
        [
            new() { Id = 1, Name = "Author A" },
            new() { Id = 2, Name = "Author B" }
        ];

        _getAuthors.Setup(x => x.Handle(It.IsAny<GetAuthorsQuery>())).ReturnsAsync(expected);

        ActionResult<List<AuthorDTO>> result = await _controller.GetAuthors();

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        Assert.That(((OkObjectResult)result.Result!).Value, Is.SameAs(expected));
    }

    [Test]
    public async Task GetAuthor_Should_Return_Ok_When_Author_Exists()
    {
        AuthorDTO expected = new() { Id = 1, Name = "George Orwell" };

        _getAuthor.Setup(x => x.Handle(new GetAuthorQuery(1))).ReturnsAsync(expected);

        ActionResult<AuthorDTO> result = await _controller.GetAuthor(1);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        Assert.That(((OkObjectResult)result.Result!).Value, Is.SameAs(expected));
    }

    [Test]
    public async Task GetAuthor_Should_Return_NotFound_When_Author_Does_Not_Exist()
    {
        _getAuthor.Setup(x => x.Handle(new GetAuthorQuery(99))).ReturnsAsync((AuthorDTO?)null);

        ActionResult<AuthorDTO> result = await _controller.GetAuthor(99);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task AddAuthor_Should_Return_Success_Response()
    {
        _create.Setup(x => x.Handle(new CreateAuthorCommand("New Author")))
            .ReturnsAsync(Result.Ok("Author added successfully."));

        ActionResult<Result> result = await _controller.AddAuthor(new AuthorDTO { Name = "New Author" });

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        Assert.That(((OkObjectResult)result.Result!).Value,
            Is.EqualTo(Result.Ok("Author added successfully.")));
    }

    [Test]
    public async Task AddAuthor_Should_Return_BadRequest_When_Command_Is_Invalid()
    {
        _create.Setup(x => x.Handle(It.IsAny<CreateAuthorCommand>()))
            .ReturnsAsync(Result.Invalid("Author name is required."));

        ActionResult<Result> result = await _controller.AddAuthor(new AuthorDTO { Name = " " });

        AssertProblemStatus(result.Result, 400);
    }

    [Test]
    public async Task UpdateAuthor_Should_Return_Success_Response()
    {
        _rename.Setup(x => x.Handle(new RenameAuthorCommand(3, "Updated Author")))
            .ReturnsAsync(Result.Ok("Author updated successfully."));

        ActionResult<Result> result =
            await _controller.UpdateAuthor(3, new AuthorDTO { Id = 3, Name = "Updated Author" });

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        Assert.That(((OkObjectResult)result.Result!).Value,
            Is.EqualTo(Result.Ok("Author updated successfully.")));
    }

    [Test]
    public async Task UpdateAuthor_Should_Return_NotFound_When_Author_Is_Missing()
    {
        _rename.Setup(x => x.Handle(It.IsAny<RenameAuthorCommand>()))
            .ReturnsAsync(Result.NotFound("Author not found."));

        ActionResult<Result> result =
            await _controller.UpdateAuthor(9, new AuthorDTO { Name = "Whoever" });

        AssertProblemStatus(result.Result, 404);
    }

    [Test]
    public async Task DeleteAuthor_Should_Return_Success_Response()
    {
        _delete.Setup(x => x.Handle(new DeleteAuthorCommand(4)))
            .ReturnsAsync(Result.Ok("Author deleted successfully."));

        ActionResult<Result> result = await _controller.DeleteAuthor(4);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        Assert.That(((OkObjectResult)result.Result!).Value,
            Is.EqualTo(Result.Ok("Author deleted successfully.")));
    }

    [Test]
    public async Task DeleteAuthor_Should_Return_Conflict_When_Author_Has_Books()
    {
        _delete.Setup(x => x.Handle(It.IsAny<DeleteAuthorCommand>()))
            .ReturnsAsync(Result.Conflict("Author cannot be deleted because the author has books."));

        ActionResult<Result> result = await _controller.DeleteAuthor(1);

        AssertProblemStatus(result.Result, 409);
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
            typeof(AuthorController).GetCustomAttribute<AuthorizeAttribute>();

        MethodInfo method = typeof(AuthorController).GetMethod(actionName)!;
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
            typeof(AuthorController).GetCustomAttribute<AuthorizeAttribute>();

        MethodInfo method = typeof(AuthorController).GetMethod(actionName)!;
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

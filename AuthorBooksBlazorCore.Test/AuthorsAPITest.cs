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
public class AuthorControllerTests
{
    private Mock<IAuthors> _authorsRepo = null!;
    private AuthorController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _authorsRepo = new Mock<IAuthors>();
        _controller = new AuthorController(_authorsRepo.Object);
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

        _authorsRepo.Setup(x => x.GetAuthors()).ReturnsAsync(expected);

        ActionResult<List<AuthorDTO>> result = await _controller.GetAuthors();

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

        OkObjectResult ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.SameAs(expected));
        _authorsRepo.Verify(x => x.GetAuthors(), Times.Once);
    }

    [Test]
    public async Task GetAuthor_Should_Return_Ok_When_Author_Exists()
    {
        AuthorDTO expected = new()
        {
            Id = 1,
            Name = "George Orwell"
        };

        _authorsRepo.Setup(x => x.GetAuthor(1)).ReturnsAsync(expected);

        ActionResult<AuthorDTO> result = await _controller.GetAuthor(1);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

        OkObjectResult ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.SameAs(expected));
        _authorsRepo.Verify(x => x.GetAuthor(1), Times.Once);
    }

    [Test]
    public async Task GetAuthor_Should_Return_NotFound_When_Author_Does_Not_Exist()
    {
        _authorsRepo.Setup(x => x.GetAuthor(99)).ReturnsAsync((AuthorDTO?)null);

        ActionResult<AuthorDTO> result = await _controller.GetAuthor(99);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        _authorsRepo.Verify(x => x.GetAuthor(99), Times.Once);
    }

    [Test]
    public async Task AddAuthor_Should_Return_Success_Response()
    {
        AuthorDTO dto = new()
        {
            Name = "New Author"
        };
        GeneralResponse expectedResponse = new(true, "Author added successfully");
        _authorsRepo.Setup(x => x.AddAuthor(dto)).ReturnsAsync(expectedResponse);

        ActionResult<GeneralResponse> result = await _controller.AddAuthor(dto);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

        OkObjectResult ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.EqualTo(expectedResponse));
        _authorsRepo.Verify(x => x.AddAuthor(dto), Times.Once);
    }

    [Test]
    public async Task UpdateAuthor_Should_Return_Success_Response()
    {
        const int id = 3;

        AuthorDTO dto = new()
        {
            Id = id,
            Name = "Updated Author"
        };

        GeneralResponse expectedResponse = new(true, "Author updated successfully");
        _authorsRepo.Setup(x => x.UpdateAuthor(id, dto)).ReturnsAsync(expectedResponse);

        ActionResult<GeneralResponse> result = await _controller.UpdateAuthor(id, dto);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

        OkObjectResult ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.EqualTo(expectedResponse));
        _authorsRepo.Verify(x => x.UpdateAuthor(id, dto), Times.Once);
    }

    [Test]
    public async Task DeleteAuthor_Should_Return_Success_Response()
    {
        const int id = 4;
        GeneralResponse expected = new(true, "Author deleted successfully");

        _authorsRepo.Setup(x => x.DeleteAuthor(id)).ReturnsAsync(expected);

        ActionResult<GeneralResponse> result = await _controller.DeleteAuthor(id);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

        OkObjectResult ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.EqualTo(expected));
        _authorsRepo.Verify(x => x.DeleteAuthor(id), Times.Once);
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
using BlazorWebAssemblyApp.Services;
using SharedClassLibrary.Models;
using System.Net;
using System.Text;

namespace AuthorBooksBlazorCore.Test;

[TestFixture]
public class ResultReaderTests
{
    private const string DefaultSuccess = "default ok";
    private const string DefaultFailure = "default failure";

    [Test]
    public async Task Success_WithResultBody_ReturnsBodyMessage()
    {
        Result result = await Read(HttpStatusCode.OK, """{"status":"Success","message":"Author added successfully."}""");

        Assert.That(result, Is.EqualTo(Result.Ok("Author added successfully.")));
    }

    [TestCase("")]
    [TestCase("{}")]
    [TestCase("""{"status":"Success","message":""}""")]
    [TestCase("not json")]
    public async Task Success_WithoutUsableMessage_ReturnsDefaultSuccess(string body)
    {
        Result result = await Read(HttpStatusCode.OK, body);

        Assert.That(result, Is.EqualTo(Result.Ok(DefaultSuccess)));
    }

    [TestCase(HttpStatusCode.NotFound, ResultStatus.NotFound)]
    [TestCase(HttpStatusCode.Conflict, ResultStatus.Conflict)]
    [TestCase(HttpStatusCode.Unauthorized, ResultStatus.Unauthorized)]
    [TestCase(HttpStatusCode.Forbidden, ResultStatus.Unauthorized)]
    [TestCase(HttpStatusCode.BadRequest, ResultStatus.Invalid)]
    [TestCase(HttpStatusCode.TooManyRequests, ResultStatus.Invalid)]
    public async Task Failure_WithProblemDetails_MapsStatusAndReturnsDetailAsMessage(HttpStatusCode http, ResultStatus expected)
    {
        Result result = await Read(http,
            """{"title":"Conflict","status":409,"detail":"Author cannot be deleted because the author has books."}""");

        Assert.That(result, Is.EqualTo(new Result(expected, "Author cannot be deleted because the author has books.")));
    }

    [Test]
    public async Task Failure_WithValidationProblem_JoinsAllFieldErrors()
    {
        Result result = await Read(HttpStatusCode.BadRequest, """
            {"title":"One or more validation errors occurred.","status":400,
             "errors":{"Name":["The Name field is required."],"Id":["Id is invalid.","Id must be positive."]}}
            """);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Message, Is.EqualTo("The Name field is required. Id is invalid. Id must be positive."));
    }

    [Test]
    public async Task Failure_WithProblemWithoutDetail_FallsBackToTitle()
    {
        Result result = await Read(HttpStatusCode.NotFound, """{"title":"Not Found","status":404}""");

        Assert.That(result, Is.EqualTo(Result.NotFound("Not Found")));
    }

    [TestCase("")]
    [TestCase("<html>Bad gateway</html>")]
    [TestCase("[1,2,3]")]
    public async Task Failure_WithUnusableBody_ReturnsDefaultFailure(string body)
    {
        Result result = await Read(HttpStatusCode.BadGateway, body);

        Assert.That(result, Is.EqualTo(Result.Invalid(DefaultFailure)));
    }

    [Test]
    public async Task Generic_Success_ReturnsValueAndBodyMessage()
    {
        Result<string> result = await ReadOf<string>(HttpStatusCode.OK, """{"status":"Success","message":"Login completed","value":"abc.def.ghi"}""");

        Assert.That(result, Is.EqualTo(Result<string>.Ok("abc.def.ghi", "Login completed")));
    }

    [TestCase("")]
    [TestCase("""{"status":"Success","message":"Login completed"}""")]
    [TestCase("not json")]
    public async Task Generic_SuccessWithoutTheValue_IsTreatedAsFailure(string body)
    {
        Result<string> result = await ReadOf<string>(HttpStatusCode.OK, body);

        Assert.That(result, Is.EqualTo(Result<string>.Invalid(DefaultFailure)));
    }

    [Test]
    public async Task Generic_Failure_MapsStatusAndMessage_WithNoValue()
    {
        Result<string> result = await ReadOf<string>(HttpStatusCode.Unauthorized,
            """{"title":"Unauthorized","status":401,"detail":"Email/Password not valid"}""");

        Assert.That(result, Is.EqualTo(Result<string>.Unauthorized("Email/Password not valid")));
        Assert.That(result.Value, Is.Null);
    }

    private static async Task<Result> Read(HttpStatusCode status, string body)
    {
        using HttpResponseMessage response = Respond(status, body);
        return await ResultReader.ToResult(response, DefaultSuccess, DefaultFailure);
    }

    private static async Task<Result<T>> ReadOf<T>(HttpStatusCode status, string body)
    {
        using HttpResponseMessage response = Respond(status, body);
        return await ResultReader.ToResult<T>(response, DefaultSuccess, DefaultFailure);
    }

    private static HttpResponseMessage Respond(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}

using BlazorWebAssemblyApp.Services;
using System.Net;
using System.Text;
using GeneralResponse = SharedClassLibrary.Models.ServiceResponses.GeneralResponse;

namespace AuthorBooksBlazorCore.Test;

[TestFixture]
public class ApiResponseReaderTests
{
    private const string DefaultSuccess = "default ok";
    private const string DefaultFailure = "default failure";

    [Test]
    public async Task Success_WithGeneralResponseBody_ReturnsBodyMessage()
    {
        GeneralResponse result = await Read(HttpStatusCode.OK, """{"flag":true,"message":"Author added successfully."}""");

        Assert.That(result, Is.EqualTo(new GeneralResponse(true, "Author added successfully.")));
    }

    [Test]
    public async Task Success_WithEmptyBody_ReturnsDefaultSuccess()
    {
        GeneralResponse result = await Read(HttpStatusCode.OK, "");

        Assert.That(result, Is.EqualTo(new GeneralResponse(true, DefaultSuccess)));
    }

    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.Conflict)]
    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.TooManyRequests)]
    public async Task Failure_WithProblemDetails_ReturnsDetailAsMessage(HttpStatusCode status)
    {
        GeneralResponse result = await Read(status,
            """{"title":"Conflict","status":409,"detail":"Author cannot be deleted because the author has books."}""");

        Assert.That(result, Is.EqualTo(new GeneralResponse(false, "Author cannot be deleted because the author has books.")));
    }

    [Test]
    public async Task Failure_WithValidationProblem_JoinsAllFieldErrors()
    {
        GeneralResponse result = await Read(HttpStatusCode.BadRequest, """
            {"title":"One or more validation errors occurred.","status":400,
             "errors":{"Name":["The Name field is required."],"Id":["Id is invalid.","Id must be positive."]}}
            """);

        Assert.That(result.Flag, Is.False);
        Assert.That(result.Message, Is.EqualTo("The Name field is required. Id is invalid. Id must be positive."));
    }

    [Test]
    public async Task Failure_WithProblemWithoutDetail_FallsBackToTitle()
    {
        GeneralResponse result = await Read(HttpStatusCode.NotFound, """{"title":"Not Found","status":404}""");

        Assert.That(result, Is.EqualTo(new GeneralResponse(false, "Not Found")));
    }

    [TestCase("")]
    [TestCase("<html>Bad gateway</html>")]
    [TestCase("[1,2,3]")]
    public async Task Failure_WithUnusableBody_ReturnsDefaultFailure(string body)
    {
        GeneralResponse result = await Read(HttpStatusCode.BadGateway, body);

        Assert.That(result, Is.EqualTo(new GeneralResponse(false, DefaultFailure)));
    }

    private static async Task<GeneralResponse> Read(HttpStatusCode status, string body)
    {
        using HttpResponseMessage response = new(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        return await ApiResponseReader.ToGeneralResponse(response, DefaultSuccess, DefaultFailure);
    }
}

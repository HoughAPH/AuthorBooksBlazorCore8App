using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedClassLibrary.Models;

namespace IdentityManagerServerApi.Features.Common;

public static class ResultExtensions
{
    /// <summary>
    /// Success: 200 with the <see cref="Result"/> itself as the body (the UI shows its message).
    /// Failure: an RFC 9457 problem details response (application/problem+json) whose status reflects the
    /// failure: 404 not found, 409 conflict, 401 unauthorized, 400 otherwise. The message is carried in <c>detail</c>.
    /// </summary>
    public static ActionResult ToActionResult(this ControllerBase controller, Result result) =>
        result.Succeeded ? controller.Ok(result) : controller.Failure(result.Status, result.Message);

    /// <summary>As <see cref="ToActionResult(ControllerBase, Result)"/>; on success the body includes the value.</summary>
    public static ActionResult ToActionResult<T>(this ControllerBase controller, Result<T> result) =>
        result.Succeeded ? controller.Ok(result) : controller.Failure(result.Status, result.Message);

    private static ObjectResult Failure(this ControllerBase controller, ResultStatus resultStatus, string message)
    {
        (int status, string title) = resultStatus switch
        {
            ResultStatus.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ResultStatus.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ResultStatus.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status400BadRequest, "Bad Request")
        };

        return controller.Problem(detail: message, statusCode: status, title: title);
    }
}

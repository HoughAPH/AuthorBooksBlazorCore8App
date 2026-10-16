using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagerServerApi.Features.Common;

public static class ResultExtensions
{
    /// <summary>
    /// Success: 200 with a GeneralResponse body (the message is shown by the client UI).
    /// Failure: an RFC 9457 problem details response (application/problem+json) whose status reflects the
    /// failure: 404 not found, 409 conflict, 400 otherwise. The message is carried in <c>detail</c>.
    /// </summary>
    public static ActionResult ToActionResult(this ControllerBase controller, Result result)
    {
        if (result.Succeeded)
            return controller.Ok(result.ToResponse());

        (int status, string title) = result.Status switch
        {
            ResultStatus.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ResultStatus.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status400BadRequest, "Bad Request")
        };

        return controller.Problem(detail: result.Message, statusCode: status, title: title);
    }
}

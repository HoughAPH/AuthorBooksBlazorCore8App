using IdentityManagerServerApi.Domain;
using SharedClassLibrary.Models;

namespace IdentityManagerServerApi.Features.Common;

public enum ResultStatus
{
    Success,
    NotFound,
    Invalid,
    Conflict
}

public readonly record struct Result(ResultStatus Status, string Message)
{
    public bool Succeeded => Status == ResultStatus.Success;

    public static Result Ok(string message) => new(ResultStatus.Success, message);
    public static Result NotFound(string message) => new(ResultStatus.NotFound, message);
    public static Result Invalid(string message) => new(ResultStatus.Invalid, message);
    public static Result Conflict(string message) => new(ResultStatus.Conflict, message);

    public static Result From(DomainException ex) => ex.Kind switch
    {
        DomainErrorKind.NotFound => NotFound(ex.Message),
        DomainErrorKind.Conflict => Conflict(ex.Message),
        _ => Invalid(ex.Message)
    };

    public ServiceResponses.GeneralResponse ToResponse() => new(Succeeded, Message);
}

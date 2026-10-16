using IdentityManagerServerApi.Domain;
using SharedClassLibrary.Models;

namespace IdentityManagerServerApi.Features.Common;

public enum ResultStatus
{
    Success,
    NotFound,
    Invalid,
    Conflict,
    Unauthorized
}

public readonly record struct Result(ResultStatus Status, string Message)
{
    public bool Succeeded => Status == ResultStatus.Success;

    public static Result Ok(string message) => new(ResultStatus.Success, message);
    public static Result NotFound(string message) => new(ResultStatus.NotFound, message);
    public static Result Invalid(string message) => new(ResultStatus.Invalid, message);
    public static Result Conflict(string message) => new(ResultStatus.Conflict, message);
    public static Result Unauthorized(string message) => new(ResultStatus.Unauthorized, message);

    public static Result From(DomainException ex) => ex.Kind switch
    {
        DomainErrorKind.NotFound => NotFound(ex.Message),
        DomainErrorKind.Conflict => Conflict(ex.Message),
        _ => Invalid(ex.Message)
    };

    public ServiceResponses.GeneralResponse ToResponse() => new(Succeeded, Message);
}

/// <summary>A <see cref="Result"/> that also carries a value on success (for example a token).</summary>
public readonly record struct Result<T>(Result Outcome, T? Value = default)
{
    public bool Succeeded => Outcome.Succeeded;

    public static Result<T> Ok(T value, string message) => new(Result.Ok(message), value);
    public static Result<T> Fail(Result failure) => new(failure);
}

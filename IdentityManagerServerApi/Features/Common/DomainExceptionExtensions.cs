using IdentityManagerServerApi.Domain;
using SharedClassLibrary.Models;

namespace IdentityManagerServerApi.Features.Common;

public static class DomainExceptionExtensions
{
    /// <summary>Maps a rule violation to the matching failed <see cref="Result"/>.</summary>
    public static Result ToResult(this DomainException ex) => ex.Kind switch
    {
        DomainErrorKind.NotFound => Result.NotFound(ex.Message),
        DomainErrorKind.Conflict => Result.Conflict(ex.Message),
        _ => Result.Invalid(ex.Message)
    };
}

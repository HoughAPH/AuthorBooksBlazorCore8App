namespace IdentityManagerServerApi.Domain;

public enum DomainErrorKind
{
    Validation,
    Conflict,
    NotFound
}

public class DomainException(string message, DomainErrorKind kind = DomainErrorKind.Validation) : Exception(message)
{
    public DomainErrorKind Kind { get; } = kind;
}

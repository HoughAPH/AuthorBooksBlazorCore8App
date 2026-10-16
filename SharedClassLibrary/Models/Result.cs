using System.Text.Json.Serialization;

namespace SharedClassLibrary.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResultStatus
{
    Success,
    Invalid,
    NotFound,
    Conflict,
    Unauthorized
}

/// <summary>
/// The outcome of an operation that returns no value. Shared by the API (handlers, response body on success) and
/// the Blazor client (service return type), so both sides speak the same type.
/// </summary>
public sealed record Result(ResultStatus Status, string Message)
{
    [JsonIgnore]
    public bool Succeeded => Status == ResultStatus.Success;

    public static Result Ok(string message = "") => new(ResultStatus.Success, message);
    public static Result Invalid(string message) => new(ResultStatus.Invalid, message);
    public static Result NotFound(string message) => new(ResultStatus.NotFound, message);
    public static Result Conflict(string message) => new(ResultStatus.Conflict, message);
    public static Result Unauthorized(string message) => new(ResultStatus.Unauthorized, message);
}

/// <summary>
/// The outcome of an operation that returns a value on success (for example the login token). Flat on purpose:
/// <c>Status</c>, <c>Message</c> and <c>Value</c> sit side by side, with <c>Value</c> only set on success.
/// </summary>
public sealed record Result<T>(ResultStatus Status, string Message, T? Value = default)
{
    [JsonIgnore]
    public bool Succeeded => Status == ResultStatus.Success;

    public static Result<T> Ok(T value, string message = "") => new(ResultStatus.Success, message, value);
    public static Result<T> Invalid(string message) => new(ResultStatus.Invalid, message);
    public static Result<T> NotFound(string message) => new(ResultStatus.NotFound, message);
    public static Result<T> Conflict(string message) => new(ResultStatus.Conflict, message);
    public static Result<T> Unauthorized(string message) => new(ResultStatus.Unauthorized, message);

    /// <summary>Carries a failed non-generic <see cref="Result"/> over to a <see cref="Result{T}"/>.</summary>
    public static Result<T> From(Result failure) => new(failure.Status, failure.Message);
}

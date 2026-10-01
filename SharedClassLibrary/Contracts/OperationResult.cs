namespace SharedClassLibrary.Contracts;

public class OperationResult
{
    public bool IsSuccess { get; protected init; }
    public ErrorType ErrorType { get; protected init; } = ErrorType.None;
    public string Message { get; protected init; } = string.Empty;

    public static OperationResult Success(string message = "") => new()
    {
        IsSuccess = true,
        ErrorType = ErrorType.None,
        Message = message
    };

    public static OperationResult Failure(ErrorType errorType, string message) => new()
    {
        IsSuccess = false,
        ErrorType = errorType,
        Message = message
    };
}

public class OperationResult<T> : OperationResult
{
    public T? Value { get; private init; }

    public static OperationResult<T> Success(T? value, string message = "") => new()
    {
        IsSuccess = true,
        ErrorType = ErrorType.None,
        Message = message,
        Value = value
    };

    public static new OperationResult<T> Failure(ErrorType errorType, string message) => new()
    {
        IsSuccess = false,
        ErrorType = errorType,
        Message = message,
        Value = default
    };
}

public enum ErrorType
{
    None,
    NotFound,
    ValidationError,
    Conflict,
    ServerError
}
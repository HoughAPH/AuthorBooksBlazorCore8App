namespace SharedClassLibrary.DTOs
{
    public class ServiceResponses
    {
        public record class GeneralResponse(bool Flag, string Message);
        public record class LoginResponse(bool Flag, string Token, string Message);

    }



    //public class Response<T>(bool success, string? message, T? data)
    //{
    //    public string? Message { get; set; }
    //    //public string? ValidationErrors { get; set; }
    //    public bool Success { get; set; }
    //    public T? Data { get; set; }
    //}

}
//public sealed record Error(string Code, string? Description = null)
//{
//    public static readonly Error None = new(string.Empty);
//    public static implicit operator Result(Error error) => Result.Failure(error);
//}

//public static class AuthorErrors
//{
//    public static readonly Error NotFound = new("Authors.NotFound", "Author does not exist");
//    public static readonly Error NullValue = new("Authors.NullValue", "Author may not be null");
//    public static readonly Error InvalidId = new("Authors.InvalidId", "Author Id is invalid");
//    public static readonly Error InvalidName = new("Authors.InvalidName", "Author Name is invalid");
//    public static readonly Error InvalidEmail = new("Authors.InvalidEmail", "Author Email is invalid");
//    public static readonly Error InvalidPassword = new("Authors.InvalidPassword", "Author Password is invalid");
//    public static readonly Error EmailNotUnique = new("Authors.EmailNotUnique", "The email already exists");


//}


//public class Result
//{
//    private Result(bool isSuccess, Error error)
//    {
//        if (isSuccess && error != Error.None
//            || isSuccess && error == Error.None)
//        {
//            throw new ArgumentException("Invalid operation", nameof(error));
//        }
//        IsSuccess = isSuccess;
//        Error = error;
//    }
//    public bool IsSuccess { get; }
//    public bool IsFailure => !IsSuccess;
//    public Error Error { get; }

//    public static Result Success() => new(true, Error.None);
//    public static Result Failure(Error error) => new(false, error);


//    public record Error
//    {
//        public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
//        public static readonly Error NullValue = new("Error.NullValue", "Provided value may not be null", ErrorType.Failure);

//        private Error(string code, string description, ErrorType type)
//        {
//            Code = code;
//            Description = description;
//            Type = type;
//        }

//        public static Error NotFound(string code, string description ) =>
//            new(code, description, ErrorType.NotFound);
//        public static Error Validation(string code, string description) =>
//            new(code, description, ErrorType.Validation);
//        public static Error Conflict(string code, string description) =>
//            new(code, description, ErrorType.Conflict);
//        public static Error Unauthorized(string code, string description) =>
//            new(code, description, ErrorType.Unauthorized);
//        public static Error Failure(string code, string description) =>
//            new(code, description, ErrorType.Failure);
//        public static Error Conflict(string description) =>
//            new("Error.Conflict", description, ErrorType.Conflict);

//        public string Code { get; }
//        public string Description { get; }
//        public ErrorType Type { get; }
//    }
//}

//public enum ErrorType
//{
//    Failure = 0,
//    Validation = 1,
//    NotFound = 2,
//    Conflict = 3,
//    Unauthorized = 4,
//}


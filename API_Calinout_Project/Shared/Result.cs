namespace API_Calinout_Project.Shared
{

    public enum ErrorType
    {
        None,
        NotSpecific, // local
        Validation, // 400
        NotFound,   // 404
        Conflict,   // 409
        Forbidden,  // 403
        Unauthorized, // 401
        Server      // 500
    }

    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public string? Error { get; }

        public ErrorType ErrorType { get; } = ErrorType.NotSpecific;

        private Result(bool isSuccess, T? value, string? error, ErrorType errorType)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
            ErrorType = errorType;
        }

        // Factory Methods
        public static Result<T> Success(T value) => new(true, value, null, ErrorType.None);
        public static Result<T> Failure(string error, ErrorType errorType = ErrorType.NotSpecific) => new(false, default, error, errorType);
    }

    // Non-generic version for void operations
    public class Result
    {
        public bool IsSuccess { get; }
        public string? Error { get; }
        public ErrorType ErrorType { get; }

        private Result(bool isSuccess, string? error, ErrorType errorType)
        {
            IsSuccess = isSuccess;
            Error = error;
            ErrorType = errorType;
        }

        public static Result Success() => new(true, null, ErrorType.None);
        public static Result Failure(string error, ErrorType errorType = ErrorType.NotSpecific) => new(false, error, errorType);
    }
}
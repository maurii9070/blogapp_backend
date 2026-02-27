namespace Blog.Api.Shared;

public class Result<T>
{   
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    
    public IDictionary<string, string[]>? ValidationErrors { get; }

    private Result(bool isSuccess, T? value, string? error, IDictionary<string, string[]>? validationErrors = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ValidationErrors = validationErrors;
    }

    public static Result<T> Success(T value) => new Result<T>(true, value, null);
    public static Result<T> Failure(string error) => new Result<T>(false, default, error);
    public static Result<T> ValidationFailure(IDictionary<string, string[]> validationErrors) 
        => new Result<T>(false, default, "Validation Failed", validationErrors);
}
namespace Shuttlez.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public string? Code { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static ApiResponse<T> Ok(T data, string? message = null, string? code = null) => new()
    {
        Success = true,
        Data = data,
        Message = message,
        Code = code
    };

    public static ApiResponse<T> Fail(string error, string? code = null, params string[] errors) => new()
    {
        Success = false,
        Message = error,
        Code = code,
        Errors = errors.Length > 0 ? errors : [error]
    };
}

public class ApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? Code { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static ApiResponse Ok(string? message = null, string? code = null) => new()
    {
        Success = true,
        Message = message,
        Code = code
    };

    public static ApiResponse Fail(string error, string? code = null) => new()
    {
        Success = false,
        Message = error,
        Code = code,
        Errors = [error]
    };
}

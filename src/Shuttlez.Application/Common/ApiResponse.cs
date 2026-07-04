namespace Shuttlez.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    public static ApiResponse<T> Fail(string error, params string[] errors) => new()
    {
        Success = false,
        Message = error,
        Errors = errors.Length > 0 ? errors : [error]
    };
}

public class ApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static ApiResponse Ok(string? message = null) => new()
    {
        Success = true,
        Message = message
    };

    public static ApiResponse Fail(string error) => new()
    {
        Success = false,
        Message = error,
        Errors = [error]
    };
}

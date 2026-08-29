namespace Shuttlez.Application.Common;

public class AppException : Exception
{
    public int StatusCode { get; }
    public string? Code { get; }

    public AppException(string message, int statusCode = 400, string? code = null) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message, string? code = null) : base(message, 404, code) { }
}

public class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message, string? code = null) : base(message, 401, code) { }
}

public class ForbiddenAppException : AppException
{
    public ForbiddenAppException(string message, string? code = null) : base(message, 403, code) { }
}

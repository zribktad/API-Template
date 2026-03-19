namespace APITemplate.Domain.Exceptions;

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message, string? errorCode = null)
        : base(message, errorCode) { }
}

namespace PoolHub.Shared.Exceptions;

public abstract class AppException(string message, IEnumerable<string>? errors = null) : Exception(message)
{
    public IReadOnlyCollection<string> Errors { get; } = errors?.ToArray() ?? [];
}

public sealed class ValidationException(string message) : AppException(message);
public sealed class UnauthorizedException(string message) : AppException(message);
public sealed class ForbiddenException(string message) : AppException(message);
public sealed class LockedException(string message) : AppException(message);
public sealed class NotFoundException(string message) : AppException(message);
public sealed class ConflictException(string message, IEnumerable<string>? errors = null) : AppException(message, errors);
public sealed class BusinessRuleException(string message) : AppException(message);
public sealed class ServiceUnavailableException(string message) : AppException(message);

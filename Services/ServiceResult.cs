namespace BookTracker.Services;

public enum ServiceErrorType
{
    None,
    NotFound,
    Validation,
    Conflict,
    Forbidden
}

/// <summary>
/// Результат операції сервісу: успіх або тип помилки з повідомленням для користувача.
/// </summary>
public class ServiceResult
{
    public ServiceErrorType ErrorType { get; private init; }
    public string? Error { get; private init; }
    public bool Succeeded => ErrorType == ServiceErrorType.None;

    public static ServiceResult Success() => new();

    public static ServiceResult NotFound(string message) =>
        new() { ErrorType = ServiceErrorType.NotFound, Error = message };

    public static ServiceResult Invalid(string message) =>
        new() { ErrorType = ServiceErrorType.Validation, Error = message };

    public static ServiceResult Conflict(string message) =>
        new() { ErrorType = ServiceErrorType.Conflict, Error = message };

    public static ServiceResult Forbidden(string message) =>
        new() { ErrorType = ServiceErrorType.Forbidden, Error = message };
}

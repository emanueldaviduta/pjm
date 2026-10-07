namespace ProjectManager.Application.Common;

/// <summary>Outcome of a service call. The Api layer maps it to an HTTP response.</summary>
public enum ServiceStatus
{
    Ok,
    NotFound,
    Conflict,
    Invalid,
    Unauthorized
}

public record ServiceResult(ServiceStatus Status, string? Message = null)
{
    public bool IsOk => Status == ServiceStatus.Ok;

    public static ServiceResult Ok() => new(ServiceStatus.Ok);
    public static ServiceResult NotFound(string? message = null) => new(ServiceStatus.NotFound, message);
    public static ServiceResult Conflict(string? message = null) => new(ServiceStatus.Conflict, message);
    public static ServiceResult Invalid(string message) => new(ServiceStatus.Invalid, message);
    public static ServiceResult Unauthorized() => new(ServiceStatus.Unauthorized);
}

public record ServiceResult<T>(ServiceStatus Status, T? Value = default, string? Message = null)
{
    public bool IsOk => Status == ServiceStatus.Ok;

    public static ServiceResult<T> Ok(T value) => new(ServiceStatus.Ok, value);
    public static ServiceResult<T> NotFound(string? message = null) => new(ServiceStatus.NotFound, default, message);
    public static ServiceResult<T> Conflict(string? message = null) => new(ServiceStatus.Conflict, default, message);
    public static ServiceResult<T> Invalid(string message) => new(ServiceStatus.Invalid, default, message);
    public static ServiceResult<T> Unauthorized() => new(ServiceStatus.Unauthorized);
}

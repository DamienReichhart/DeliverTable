namespace DeliverTableServer.Common;

/// <summary>
///     Represents a typed error returned by a service operation.
///     Carries a user-facing message and the HTTP status code the controller should return.
/// </summary>
public sealed record ServiceError(string Message, int StatusCode = 400)
{
    public static ServiceError BadRequest(string message) => new(message, 400);

    public static ServiceError Unauthorized(string message) => new(message, 401);

    public static ServiceError Forbidden(string message) => new(message, 403);

    public static ServiceError NotFound(string message) => new(message, 404);

    public static ServiceError Conflict(string message) => new(message, 409);
}

/// <summary>
///     Discriminated result for service operations that produce no value.
///     Use <see cref="ServiceResult{T}"/> when a value is expected on success.
/// </summary>
public sealed class ServiceResult
{
    public ServiceError? Error { get; }
    public bool IsSuccess => Error is null;

    private ServiceResult() { }
    private ServiceResult(ServiceError error) => Error = error;

    public static ServiceResult Success() => new();
    public static ServiceResult Failure(ServiceError error) => new(error);

    public static implicit operator ServiceResult(ServiceError error) => Failure(error);
}

/// <summary>
///     Discriminated result for service operations that produce a value on success.
///     Implicit conversions allow returning <typeparamref name="T"/> or <see cref="ServiceError"/> directly.
/// </summary>
public sealed class ServiceResult<T>
{
    public T? Value { get; }
    public ServiceError? Error { get; }
    public bool IsSuccess => Error is null;

    private ServiceResult(T value) => Value = value;
    private ServiceResult(ServiceError error) => Error = error;

    public static ServiceResult<T> Success(T value) => new(value);
    public static ServiceResult<T> Failure(ServiceError error) => new(error);

    public static implicit operator ServiceResult<T>(T value) => Success(value);
    public static implicit operator ServiceResult<T>(ServiceError error) => Failure(error);
}

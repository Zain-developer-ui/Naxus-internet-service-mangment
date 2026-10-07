using System.Text.Json.Serialization;

namespace NEXUS.Common;

/**
 * Every service method returns this. Controllers switch on Kind to pick the
 * response - exceptions are reserved for genuinely unexpected failures.
 */
public enum ErrorKind
{
    None,
    NotFound,
    Validation,
    Conflict,
    Unauthorized,
    Forbidden,
    Failure
}

public sealed class Result<T>
{
    private static readonly Result<T> _ok = new(true, default, ErrorKind.None, null, null);

    [JsonIgnore]
    public bool IsSuccess { get; }

    public T? Value { get; }

    public ErrorKind Kind { get; }

    public string? Message { get; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    private Result(bool ok, T? value, ErrorKind kind, string? message,
                   IReadOnlyDictionary<string, string[]>? errors)
    {
        IsSuccess = ok;
        Value = value;
        Kind = kind;
        Message = message;
        Errors = errors;
    }

    private Result(T? value) : this(true, value, ErrorKind.None, null, null) { }

    private Result(ErrorKind kind, string message,
                   IReadOnlyDictionary<string, string[]>? errors = null)
        : this(false, default, kind, message, errors) { }

    public static Result<T> Ok(T value) => new(value);

    public static Result<T> Fail(ErrorKind kind, string message) => new(kind, message);

    public static Result<T> Fail(string field, string message) =>
        new(ErrorKind.Validation, "Validation failed",
            new Dictionary<string, string[]> { [field] = new[] { message } });

    public static Result<T> NotFound(string what = "Record") =>
        new(ErrorKind.NotFound, $"{what} not found.");

    public static Result<T> Forbidden(string what = "this resource") =>
        new(ErrorKind.Forbidden, $"You do not have access to {what}.");

    public static Result<T> Conflict(string message) => new(ErrorKind.Conflict, message);

    public static Result<T> Unauthorized(string message = "Sign in required.") =>
        new(ErrorKind.Unauthorized, message);

    public static Result<T> Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(ErrorKind.Validation, "Validation failed", errors);

    public Result<TOut> Cast<TOut>() => IsSuccess
        ? Result<TOut>.Ok(default!)
        : Result<TOut>.Invalid(Errors ?? new Dictionary<string, string[]>());
}

public static class Result
{
    public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);

    public static Result<T> Fail<T>(ErrorKind kind, string message) => Result<T>.Fail(kind, message);

    public static Result<T> NotFound<T>(string what = "Record") => Result<T>.NotFound(what);

    public static Result<T> Forbidden<T>(string what = "this resource") => Result<T>.Forbidden(what);
}

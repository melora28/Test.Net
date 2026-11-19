namespace RoomHub.Api.Domain.Common;

public enum ErrorType { Validation, NotFound, Conflict }

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
}

public sealed class Unit
{
    public static readonly Unit Value = new();
    private Unit() { }
}

public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T? value, Error? error) { _value = value; _error = error; }

    public bool IsSuccess => _error is null;
    public bool IsFailure => !IsSuccess;
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Result is failure.");
    public Error Error => IsFailure ? _error! : throw new InvalidOperationException("Result is success.");

    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(Error error) => new(default, error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
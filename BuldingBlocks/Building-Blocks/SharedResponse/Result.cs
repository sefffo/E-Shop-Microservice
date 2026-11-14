namespace Building_Blocks.SharedResponse;

public class Result
{
    public bool IsSuccess => Errors.Count == 0;
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Error> Errors { get; }

    protected Result()
    {
        Errors = Array.Empty<Error>();
    }

    protected Result(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var errorArray = errors.ToArray();

        if (errorArray.Length == 0 || errorArray.Any(error => error is null))
        {
            throw new ArgumentException("A failed result must contain at least one non-null error.", nameof(errors));
        }

        Errors = Array.AsReadOnly(errorArray);
    }

    public static Result Ok() => new();
    public static Result Fail(Error error) => new([error]);
    public static Result Fail(IEnumerable<Error> errors) => new(errors);
}

public sealed class Result<T> : Result
{
    private readonly T _value;

    private Result(T value)
    {
        _value = value;
    }

    private Result(IEnumerable<Error> errors) : base(errors)
    {
        _value = default!;
    }

    public T GetValueOrThrow()
    {
        if (IsFailure)
        {
            throw new InvalidOperationException("Cannot get the value of a failed result. Check Errors for details.");
        }

        return _value;
    }

    public static Result<T> Ok(T value) => new(value);
    public new static Result<T> Fail(Error error) => new([error]);
    public new static Result<T> Fail(IEnumerable<Error> errors) => new(errors);

    public static implicit operator Result<T>(T value) => Ok(value);
    public static implicit operator Result<T>(Error error) => Fail(error);
}

namespace Building_Blocks.SharedResponse;

public enum ErrorType : byte
{
    Failure,
    Validation,
    NotFound,
    Unauthorized,
    Forbidden,
    InternalServerError,
    BadRequest,
    InvalidCredentials
}

public sealed record Error
{
    public string Code { get; }
    public string Description { get; }
    public ErrorType Type { get; }

    private Error(string code, string description, ErrorType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Code = code;
        Description = description;
        Type = type;
    }

    public static Error Failure(string code = "General.Failure", string description = "A general failure occurred.")
        => new(code, description, ErrorType.Failure);

    public static Error Validation(string code, string description)
        => new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description)
        => new(code, description, ErrorType.NotFound);

    public static Error Unauthorized(string code, string description)
        => new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description)
        => new(code, description, ErrorType.Forbidden);

    public static Error InternalServerError(string code, string description)
        => new(code, description, ErrorType.InternalServerError);

    public static Error BadRequest(string code, string description)
        => new(code, description, ErrorType.BadRequest);

    public static Error InvalidCredentials(string code, string description)
        => new(code, description, ErrorType.InvalidCredentials);
}

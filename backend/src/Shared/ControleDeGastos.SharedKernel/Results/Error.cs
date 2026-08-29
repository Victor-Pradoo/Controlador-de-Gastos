namespace ControleDeGastos.SharedKernel.Results;

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    External = 5,
}

/// <summary>
/// Erro de negocio. O host traduz <see cref="Type"/> em status HTTP (ver ResultExtensions na Api).
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error External(string code, string message) => new(code, message, ErrorType.External);
}

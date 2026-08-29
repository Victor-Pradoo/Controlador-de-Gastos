namespace ControleDeGastos.SharedKernel.Results;

/// <summary>
/// Resultado explicito de uma operacao. Excecoes ficam reservadas para falhas
/// inesperadas; regra de negocio violada vira <see cref="Error"/>.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Resultado de sucesso nao pode carregar erro.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Resultado de falha precisa de um erro.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error) => _value = value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Nao ha valor em um resultado de falha.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}

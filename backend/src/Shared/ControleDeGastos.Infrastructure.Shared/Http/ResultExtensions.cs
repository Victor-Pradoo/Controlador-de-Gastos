using ControleDeGastos.SharedKernel.Results;
using Microsoft.AspNetCore.Http;

namespace ControleDeGastos.Infrastructure.Shared.Http;

/// <summary>
/// Traducao unica de <see cref="Result"/> para HTTP. Endpoint nenhum decide
/// status code na mao - o tipo do erro manda.
/// </summary>
public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result, Func<IResult>? onSuccess = null) =>
        result.IsSuccess
            ? onSuccess?.Invoke() ?? Results.NoContent()
            : Problem(result.Error);

    public static IResult ToHttpResult<TValue>(this Result<TValue> result, Func<TValue, IResult>? onSuccess = null) =>
        result.IsSuccess
            ? onSuccess?.Invoke(result.Value) ?? Results.Ok(result.Value)
            : Problem(result.Error);

    private static IResult Problem(Error error) => Results.Problem(
        title: error.Code,
        detail: error.Message,
        statusCode: error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.External => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError,
        });
}

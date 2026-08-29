namespace ControleDeGastos.SharedKernel.Abstractions;

/// <summary>
/// Usuario da requisicao atual. Todo dado do app e escopado por UserId
/// desde o inicio, mesmo enquanto o MVP roda com um usuario fixo de desenvolvimento.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }

    bool IsAuthenticated { get; }
}

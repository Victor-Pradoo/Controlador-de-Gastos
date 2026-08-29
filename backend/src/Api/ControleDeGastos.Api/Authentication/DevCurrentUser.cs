using ControleDeGastos.SharedKernel.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ControleDeGastos.Api.Authentication;

/// <summary>
/// Usuario atual do MVP.
///
/// TEMPORARIO: enquanto nao ha login, resolve um usuario fixo de desenvolvimento
/// (configuravel em Auth:DevUserId) e aceita o header X-User-Id para testes multiusuario.
/// Quando o modulo de identidade entrar, esta classe le o claim "sub" do JWT e
/// NADA no resto do sistema muda - todos ja dependem de ICurrentUser.
/// </summary>
public sealed class DevCurrentUser(IHttpContextAccessor accessor, Guid devUserId) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var header = accessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();

            return Guid.TryParse(header, out var fromHeader) ? fromHeader : devUserId;
        }
    }

    public bool IsAuthenticated => true;
}

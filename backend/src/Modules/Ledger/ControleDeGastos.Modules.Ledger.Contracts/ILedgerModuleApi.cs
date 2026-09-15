using ControleDeGastos.SharedKernel.Primitives;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Ledger.Contracts;

/// <summary>
/// API publica do modulo Ledger. Outros modulos dependem SOMENTE desta interface,
/// nunca do DbContext nem das entidades do Ledger.
/// </summary>
public interface ILedgerModuleApi
{
    Task<Result<Guid>> RegisterAsync(RegisterTransactionRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionDto>> GetByMonthAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default);

    Task<MonthlyTotalsDto> GetMonthlyTotalsAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryTotalDto>> GetCategoryTotalsAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default);

    /// <summary>Ja existe lancamento com este id externo? Evita duplicar na sincronizacao bancaria.</summary>
    Task<bool> ExistsByExternalIdAsync(Guid userId, string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Regra de virada do usuario, em uma leitura so. Use isto quando for resolver
    /// varias datas ou janelas; nao reimplemente a regra do lado de fora.
    /// </summary>
    Task<CompetenceCalendarDto> GetCompetenceCalendarAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Competencia de hoje segundo a regra do usuario - o default de toda leitura sem mes.</summary>
    Task<YearMonth> GetCurrentCompetenceAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Intervalo de datas de uma competencia. Peca aqui em vez de derivar da regra do
    /// seu lado: uma segunda implementacao da mesma conta e uma segunda implementacao
    /// para divergir.
    /// </summary>
    Task<CompetenceWindowDto> GetCompetenceWindowAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upsert da ocorrencia mensal de um gasto fixo, identificada por
    /// <see cref="RegisterTransactionRequest.ExternalId"/>: cria quando nao existe e
    /// reposiciona a data quando a regra de virada mudou e a ocorrencia saiu da janela
    /// da sua competencia. Diferente de <see cref="RegisterAsync"/>, que rejeita duplicado -
    /// aqui a identidade e (gasto fixo, competencia), e ela nao muda quando a data muda.
    /// </summary>
    Task<Result> SyncRecurrenceOccurrenceAsync(RegisterTransactionRequest request, CancellationToken cancellationToken = default);
}

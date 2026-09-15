using ControleDeGastos.Modules.Ledger.Application.Transactions;
using ControleDeGastos.Modules.Ledger.Contracts;

namespace ControleDeGastos.Modules.Ledger.Tests;

/// <summary>
/// A identidade de uma ocorrencia de gasto fixo e (gasto fixo, competencia), e a data
/// dela depende da regra de virada. Se o usuario muda a regra, a ocorrencia precisa ser
/// MOVIDA de volta para dentro da janela - rejeitar como duplicado deixaria a
/// competencia sem o fixo para sempre.
/// </summary>
public sealed class SyncRecurrenceOccurrenceHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid RecurrenceId = Guid.NewGuid();
    private const string ExternalId = "recurrence:abc:2026-08";

    private static (SyncRecurrenceOccurrenceHandler Handler, InMemoryTransactionRepository Repository) Build()
    {
        var repository = new InMemoryTransactionRepository();
        return (new SyncRecurrenceOccurrenceHandler(repository, new FakeUnitOfWork()), repository);
    }

    private static RegisterTransactionRequest Request(DateOnly occurredOn, string? externalId = ExternalId) =>
        new(
            UserId,
            TransactionKind.FixedExpense,
            TransactionSource.Recurrence,
            "Aluguel",
            1500m,
            "Moradia",
            occurredOn,
            externalId,
            RecurrenceId);

    [Fact]
    public async Task Cria_a_ocorrencia_quando_ela_nao_existe()
    {
        var (handler, repository) = Build();

        var result = await handler.HandleAsync(Request(new DateOnly(2026, 8, 28)));

        Assert.True(result.IsSuccess);
        var created = Assert.Single(repository.Items);
        Assert.Equal(new DateOnly(2026, 8, 28), created.OccurredOn);
        Assert.Equal(ExternalId, created.ExternalId);
    }

    [Fact]
    public async Task Reposiciona_a_ocorrencia_quando_a_data_mudou()
    {
        var (handler, repository) = Build();
        await handler.HandleAsync(Request(new DateOnly(2026, 8, 28)));

        // Usuario passou a usar dia de virada 25: a ocorrencia de 2026-08 precisa
        // recuar para dentro da nova janela.
        var result = await handler.HandleAsync(Request(new DateOnly(2026, 8, 20)));

        Assert.True(result.IsSuccess);
        var moved = Assert.Single(repository.Items);
        Assert.Equal(new DateOnly(2026, 8, 20), moved.OccurredOn);
    }

    [Fact]
    public async Task Nao_altera_a_ocorrencia_que_ja_esta_no_lugar()
    {
        var (handler, repository) = Build();
        await handler.HandleAsync(Request(new DateOnly(2026, 8, 28)));
        var created = repository.Items[0];

        await handler.HandleAsync(Request(new DateOnly(2026, 8, 28)));

        Assert.Single(repository.Items);
        Assert.Same(created, repository.Items[0]);
        Assert.Equal(new DateOnly(2026, 8, 28), repository.Items[0].OccurredOn);
    }

    [Fact]
    public async Task Materializar_duas_vezes_nao_duplica()
    {
        var (handler, repository) = Build();

        await handler.HandleAsync(Request(new DateOnly(2026, 8, 28)));
        await handler.HandleAsync(Request(new DateOnly(2026, 8, 28)));

        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task Competencias_diferentes_sao_ocorrencias_diferentes()
    {
        var (handler, repository) = Build();

        await handler.HandleAsync(Request(new DateOnly(2026, 8, 28), "recurrence:abc:2026-08"));
        await handler.HandleAsync(Request(new DateOnly(2026, 9, 28), "recurrence:abc:2026-09"));

        Assert.Equal(2, repository.Items.Count);
    }

    [Fact]
    public async Task Ocorrencia_sem_id_externo_e_rejeitada()
    {
        var (handler, repository) = Build();

        var result = await handler.HandleAsync(Request(new DateOnly(2026, 8, 28), externalId: null));

        Assert.True(result.IsFailure);
        Assert.Equal("ledger.external_id_required", result.Error.Code);
        Assert.Empty(repository.Items);
    }

    [Fact]
    public async Task Lancamento_manual_nao_e_reposicionavel()
    {
        var (handler, repository) = Build();
        var manual = Domain.Transaction.Register(
            UserId,
            TransactionKind.Expense,
            TransactionSource.Manual,
            "Almoco",
            42m,
            "Alimentacao",
            new DateOnly(2026, 8, 28),
            ExternalId).Value;
        repository.Add(manual);

        var result = await handler.HandleAsync(Request(new DateOnly(2026, 8, 20)));

        Assert.True(result.IsFailure);
        Assert.Equal("ledger.transaction_not_repositionable", result.Error.Code);
        Assert.Equal(new DateOnly(2026, 8, 28), manual.OccurredOn);
    }
}

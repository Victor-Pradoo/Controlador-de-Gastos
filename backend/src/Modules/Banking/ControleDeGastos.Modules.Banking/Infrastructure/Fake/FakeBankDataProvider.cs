using ControleDeGastos.Modules.Banking.Application.Abstractions;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Banking.Infrastructure.Fake;

/// <summary>
/// Provedor sintetico para desenvolvimento e testes. Gera sempre o mesmo extrato
/// para um dado itemId (seed deterministico), entao o sync e reproduzivel.
/// </summary>
public sealed class FakeBankDataProvider : IBankDataProvider
{
    private static readonly (string Description, decimal Amount, BankTransactionDirection Direction)[] Samples =
    [
        ("IFOOD *RESTAURANTE SP", 47.90m, BankTransactionDirection.Debit),
        ("UBER *TRIP", 23.40m, BankTransactionDirection.Debit),
        ("SUPERMERCADO PAO DE ACUCAR", 312.75m, BankTransactionDirection.Debit),
        ("NETFLIX.COM", 44.90m, BankTransactionDirection.Debit),
        ("DROGARIA SAO PAULO", 89.30m, BankTransactionDirection.Debit),
        ("POSTO SHELL AV PAULISTA", 200.00m, BankTransactionDirection.Debit),
        ("PIX RECEBIDO - JOAO", 150.00m, BankTransactionDirection.Credit),
        ("CINEMARK SHOPPING", 68.00m, BankTransactionDirection.Debit),
        ("SPOTIFY", 21.90m, BankTransactionDirection.Debit),
        ("PADARIA ESTRELA", 18.50m, BankTransactionDirection.Debit),
    ];

    public string Name => "fake";

    public Task<Result<string>> CreateConnectTokenAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success($"fake-connect-token-{userId:N}"));

    public Task<Result<BankItemSnapshot>> GetItemAsync(string externalItemId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success(new BankItemSnapshot(externalItemId, "Banco de Testes", "UPDATED")));

    public Task<Result<IReadOnlyList<BankTransactionSnapshot>>> GetTransactionsAsync(
        string externalItemId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var results = new List<BankTransactionSnapshot>();

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            // A semente vem de (item, dia), nunca da janela consultada: um mesmo dia
            // produz sempre as mesmas transacoes, como faria um extrato de verdade.
            // Semear uma vez para o intervalo inteiro faria janelas diferentes gerarem
            // ids diferentes - e a idempotencia do sync pareceria quebrada.
            //
            // Soma dos chars em vez de GetHashCode: hash de string e aleatorizado por
            // processo, entao o extrato mudaria a cada restart da API.
            var random = new Random(externalItemId.Sum(c => (int)c) * 397 + date.DayNumber);

            // Nem todo dia tem lancamento; isso deixa o extrato falso mais realista.
            var count = random.Next(0, 3);

            for (var i = 0; i < count; i++)
            {
                var sample = Samples[random.Next(Samples.Length)];

                results.Add(new BankTransactionSnapshot(
                    ExternalId: $"{externalItemId}-{date:yyyyMMdd}-{i}",
                    Description: sample.Description,
                    Amount: sample.Amount,
                    Date: date,
                    Direction: sample.Direction));
            }
        }

        return Task.FromResult(Result.Success<IReadOnlyList<BankTransactionSnapshot>>(results));
    }
}

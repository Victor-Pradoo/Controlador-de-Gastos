using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.Modules.Ledger.Domain;
using ControleDeGastos.SharedKernel.Primitives;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Ledger.Tests;

public sealed class TransactionTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 8, 28);

    private static Result<Transaction> Register(
        string description = "Almoco",
        decimal amount = 42.50m,
        string category = "Alimentacao",
        TransactionSource source = TransactionSource.Manual) =>
        Transaction.Register(UserId, TransactionKind.Expense, source, description, amount, category, Today);

    [Fact]
    public void Lancamento_valido_e_registrado_com_valor_arredondado()
    {
        var result = Register(amount: 42.555m);

        Assert.True(result.IsSuccess);
        Assert.Equal(42.56m, result.Value.Amount.Amount);
        Assert.Equal(new YearMonth(2026, 8), result.Value.Competence);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Valor_nao_positivo_e_rejeitado(decimal amount)
    {
        var result = Register(amount: amount);

        Assert.True(result.IsFailure);
        Assert.Equal("ledger.invalid_amount", result.Error.Code);
    }

    [Fact]
    public void Descricao_vazia_e_rejeitada()
    {
        var result = Register(description: "   ");

        Assert.True(result.IsFailure);
        Assert.Equal("ledger.invalid_description", result.Error.Code);
    }

    [Fact]
    public void Descricao_e_normalizada()
    {
        var result = Register(description: "  Uber para o trabalho  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Uber para o trabalho", result.Value.Description);
    }

    [Fact]
    public void Lancamento_manual_pode_ser_editado()
    {
        Assert.True(Register().Value.IsEditable);
    }

    [Theory]
    [InlineData(TransactionSource.BankSync)]
    [InlineData(TransactionSource.Recurrence)]
    public void Lancamento_automatico_nao_pode_ser_editado(TransactionSource source)
    {
        Assert.False(Register(source: source).Value.IsEditable);
    }

    [Fact]
    public void Registro_publica_evento_de_dominio()
    {
        Assert.Single(Register().Value.DomainEvents);
    }

    [Fact]
    public void Recategorizar_troca_a_categoria()
    {
        var transaction = Register().Value;

        var result = transaction.Recategorize("Lazer");

        Assert.True(result.IsSuccess);
        Assert.Equal("Lazer", transaction.Category);
    }
}

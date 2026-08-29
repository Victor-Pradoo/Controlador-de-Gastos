namespace ControleDeGastos.Modules.Ledger.Contracts;

/// <summary>
/// Espelha os tres tipos do app legado: gasto variavel, entrada e a materializacao
/// mensal de um gasto fixo.
/// </summary>
public enum TransactionKind
{
    Expense = 1,
    Income = 2,
    FixedExpense = 3,
}

/// <summary>De onde o lancamento veio. Importado de banco nao pode ser editado a mao.</summary>
public enum TransactionSource
{
    Manual = 1,
    Recurrence = 2,
    BankSync = 3,
}

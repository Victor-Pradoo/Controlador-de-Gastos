using ControleDeGastos.SharedKernel.Abstractions;

namespace ControleDeGastos.Modules.Recurrences.Domain;

public interface IFixedExpenseRepository
{
    Task<FixedExpense?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FixedExpense>> ListAsync(Guid userId, bool onlyActive = true, CancellationToken cancellationToken = default);

    /// <summary>Usuarios com algum fixo ativo - alvo da materializacao automatica.</summary>
    Task<IReadOnlyList<Guid>> ListUserIdsWithActiveExpensesAsync(CancellationToken cancellationToken = default);

    void Add(FixedExpense fixedExpense);
}

public interface IRecurrencesUnitOfWork : IUnitOfWork;

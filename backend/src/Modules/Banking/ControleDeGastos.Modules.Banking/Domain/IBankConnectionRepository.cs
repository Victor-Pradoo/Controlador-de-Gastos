using ControleDeGastos.SharedKernel.Abstractions;

namespace ControleDeGastos.Modules.Banking.Domain;

public interface IBankConnectionRepository
{
    Task<BankConnection?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BankConnection>> ListAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByExternalItemAsync(Guid userId, string externalItemId, CancellationToken cancellationToken = default);

    void Add(BankConnection connection);
}

public interface IBankingUnitOfWork : IUnitOfWork;

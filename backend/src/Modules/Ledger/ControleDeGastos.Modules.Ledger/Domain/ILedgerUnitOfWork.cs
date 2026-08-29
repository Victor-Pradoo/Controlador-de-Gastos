using ControleDeGastos.SharedKernel.Abstractions;

namespace ControleDeGastos.Modules.Ledger.Domain;

/// <summary>
/// Unit of work DESTE modulo. Cada modulo declara a sua: registrar
/// <see cref="IUnitOfWork"/> direto no container faria um modulo salvar
/// no DbContext de outro (o ultimo registro venceria).
/// </summary>
public interface ILedgerUnitOfWork : IUnitOfWork;

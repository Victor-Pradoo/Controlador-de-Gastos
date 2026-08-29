namespace ControleDeGastos.SharedKernel.Abstractions;

/// <summary>
/// Relogio injetavel: o dominio nunca chama DateTime.Now diretamente,
/// senao "gastos do mes" fica impossivel de testar.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    DateOnly Today { get; }
}

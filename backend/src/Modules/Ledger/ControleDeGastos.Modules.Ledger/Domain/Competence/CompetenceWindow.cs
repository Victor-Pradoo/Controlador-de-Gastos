namespace ControleDeGastos.Modules.Ledger.Domain.Competence;

/// <summary>
/// Intervalo de datas que pertence a uma competencia, com as duas pontas inclusivas.
/// Substitui o par primeiro-dia/ultimo-dia do mes: com dia de virada configurado a
/// competencia atravessa dois meses do calendario.
/// </summary>
public readonly record struct CompetenceWindow(DateOnly Start, DateOnly End)
{
    /// <summary>
    /// Acontece quando duas competencias sao encerradas no mesmo dia: a do meio nao
    /// chega a receber data nenhuma. Consultar essa competencia devolve vazio, nao erro.
    /// </summary>
    public bool IsEmpty => Start > End;

    public bool Contains(DateOnly date) => !IsEmpty && date >= Start && date <= End;
}

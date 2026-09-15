using ControleDeGastos.SharedKernel.Primitives;

namespace ControleDeGastos.Modules.Ledger.Contracts;

/// <summary>Intervalo de datas de uma competencia, com as duas pontas inclusivas.</summary>
public sealed record CompetenceWindowDto(DateOnly Start, DateOnly End)
{
    /// <summary>Acontece quando duas competencias sao encerradas no mesmo dia.</summary>
    public bool IsEmpty => Start > End;

    public bool Contains(DateOnly date) => !IsEmpty && date >= Start && date <= End;
}

/// <summary>
/// Snapshot da regra de virada do usuario. Quem precisa resolver varias datas - a
/// materializacao de gastos fixos, por exemplo - pega este objeto uma vez, em vez de
/// perguntar ao Ledger data por data.
/// </summary>
public sealed record CompetenceCalendarDto(
    int? ClosingDay,
    YearMonth Current,
    CompetenceWindowDto CurrentWindow,
    IReadOnlyDictionary<YearMonth, DateOnly> Closures);

/// <summary>
/// O que a tela le: a regra vigente, onde o usuario esta e se da para desfazer um
/// encerramento. <paramref name="ReopenableCompetence"/> vem preenchido quando a
/// competencia corrente so comecou porque o usuario encerrou a anterior na mao -
/// e exatamente a competencia que o botao "reabrir" desfaz.
/// </summary>
public sealed record CompetenceSettingsDto(
    int? ClosingDay,
    YearMonth Current,
    CompetenceWindowDto CurrentWindow,
    YearMonth? ReopenableCompetence,
    DateOnly? ReopenableClosedOn);

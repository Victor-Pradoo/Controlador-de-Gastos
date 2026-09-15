/** Espelha ControleDeGastos.Modules.Ledger.Contracts. Mantenha os dois lados em sincronia. */

export interface CompetenceWindow {
  readonly start: string;
  readonly end: string;
}

export interface CompetenceSettings {
  /** Dia a partir do qual o gasto conta para a competencia seguinte. null = mes do calendario. */
  readonly closingDay: number | null;
  readonly current: string;
  readonly currentWindow: CompetenceWindow;
  /** Preenchida quando a competencia corrente so comecou por um encerramento manual. */
  readonly reopenableCompetence: string | null;
  readonly reopenableClosedOn: string | null;
}

export interface CloseCompetenceResult {
  readonly closed: string;
  readonly current: string;
}

/** Limites da regra: virada no dia 1 nao teria ponto fixo, entao comeca no 2. */
export const MIN_CLOSING_DAY = 2;
export const MAX_CLOSING_DAY = 31;

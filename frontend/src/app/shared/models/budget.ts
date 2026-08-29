export type BudgetHealth = 'Healthy' | 'Warning' | 'Critical';

export interface MonthlyBudget {
  readonly month: string;
  readonly salary: number;
  readonly reserveRate: number;
  readonly reserveAmount: number;
  readonly available: number;
  readonly fixedExpenses: number;
  readonly variableExpenses: number;
  readonly income: number;
  readonly netSpent: number;
  readonly balance: number;
  readonly consumedPercentage: number;
  readonly health: BudgetHealth;
}

export interface BudgetSettings {
  readonly salary: number;
  readonly reserveRate: number;
}

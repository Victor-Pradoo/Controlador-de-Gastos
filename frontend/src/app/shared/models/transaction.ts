/** Espelha ControleDeGastos.Modules.Ledger.Contracts. Mantenha os dois lados em sincronia. */
export type TransactionKind = 'Expense' | 'Income' | 'FixedExpense';

export type TransactionSource = 'Manual' | 'Recurrence' | 'BankSync';

export interface Transaction {
  readonly id: string;
  readonly kind: TransactionKind;
  readonly source: TransactionSource;
  readonly description: string;
  readonly amount: number;
  readonly category: string;
  readonly occurredOn: string;
  readonly isEditable: boolean;
}

export interface MonthlyTotals {
  readonly variableExpenses: number;
  readonly fixedExpenses: number;
  readonly income: number;
  readonly netSpent: number;
}

export interface CategoryTotal {
  readonly category: string;
  readonly total: number;
}

export interface LedgerSummary {
  readonly totals: MonthlyTotals;
  readonly categories: readonly CategoryTotal[];
}

export interface CreateTransaction {
  readonly kind: TransactionKind;
  readonly description: string;
  readonly amount: number;
  readonly category: string;
  readonly occurredOn: string;
}

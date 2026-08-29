export interface FixedExpense {
  readonly id: string;
  readonly description: string;
  readonly amount: number;
  readonly category: string;
  readonly dayOfMonth: number;
  readonly isActive: boolean;
}

export interface CreateFixedExpense {
  readonly description: string;
  readonly amount: number;
  readonly category: string;
  readonly dayOfMonth: number;
}

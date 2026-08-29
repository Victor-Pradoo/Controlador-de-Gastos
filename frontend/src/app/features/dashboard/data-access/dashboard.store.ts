import { Injectable, computed, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { MonthService } from '../../../shared/month.service';
import { MonthlyBudget } from '../../../shared/models/budget';
import { CategoryTotal } from '../../../shared/models/transaction';
import { TransactionsApi } from '../../transactions/data-access/transactions.api';
import { BudgetApi } from './budget.api';

@Injectable({ providedIn: 'root' })
export class DashboardStore {
  private readonly budgetApi = inject(BudgetApi);
  private readonly ledgerApi = inject(TransactionsApi);
  private readonly months = inject(MonthService);

  private readonly currentBudget = signal<MonthlyBudget | null>(null);
  private readonly currentCategories = signal<readonly CategoryTotal[]>([]);
  private readonly busy = signal(false);

  readonly budget = this.currentBudget.asReadonly();
  readonly loading = this.busy.asReadonly();

  /** Maior categoria define a escala das barras, como no app legado. */
  readonly categories = computed(() => {
    const categories = this.currentCategories();
    const max = Math.max(...categories.map((c) => c.total), 1);

    return categories.map((category) => ({
      ...category,
      percentage: (category.total / max) * 100,
    }));
  });

  load(): void {
    const month = this.months.month();
    this.busy.set(true);

    forkJoin({
      budget: this.budgetApi.monthly(month),
      summary: this.ledgerApi.summary(month),
    }).subscribe({
      next: ({ budget, summary }) => {
        this.currentBudget.set(budget);
        this.currentCategories.set(summary.categories);
        this.busy.set(false);
      },
      error: () => this.busy.set(false),
    });
  }
}

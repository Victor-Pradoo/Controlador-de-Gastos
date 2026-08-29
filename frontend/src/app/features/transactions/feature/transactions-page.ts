import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MonthService } from '../../../shared/month.service';

import { EmptyStateComponent } from '../../../shared/ui/empty-state';
import { TransactionFilter, TransactionsStore } from '../data-access/transactions.store';
import { TransactionCardComponent } from '../ui/transaction-card';

@Component({
  selector: 'app-transactions-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, EmptyStateComponent, TransactionCardComponent],
  templateUrl: './transactions-page.html',
})
export class TransactionsPage {
  private readonly months = inject(MonthService);

  protected readonly store = inject(TransactionsStore);

  constructor() {
    effect(() => {
      this.months.month();
      this.store.load();
    });
  }

  protected isActive(filter: TransactionFilter): boolean {
    const current = this.store.filter();

    return typeof filter === 'string'
      ? current === filter
      : typeof current === 'object' && current.category === filter.category;
  }

  protected remove(id: string): void {
    if (confirm('Remover este lancamento?')) {
      this.store.remove(id);
    }
  }
}

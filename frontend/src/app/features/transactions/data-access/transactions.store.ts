import { Injectable, computed, inject, signal } from '@angular/core';
import { MonthService } from '../../../shared/month.service';
import { ToastService } from '../../../shared/ui/toast.service';
import { CreateTransaction, Transaction, TransactionKind } from '../../../shared/models/transaction';
import { TransactionsApi } from './transactions.api';

export type TransactionFilter = 'all' | TransactionKind | { category: string };

/**
 * Estado da feature de lancamentos. Store por feature (nao global):
 * quem nao abre a tela nao carrega o estado dela.
 */
@Injectable({ providedIn: 'root' })
export class TransactionsStore {
  private readonly api = inject(TransactionsApi);
  private readonly months = inject(MonthService);
  private readonly toasts = inject(ToastService);

  private readonly items = signal<readonly Transaction[]>([]);
  private readonly busy = signal(false);
  private readonly activeFilter = signal<TransactionFilter>('all');

  readonly loading = this.busy.asReadonly();
  readonly filter = this.activeFilter.asReadonly();

  readonly transactions = computed(() => {
    const filter = this.activeFilter();
    const all = this.items();

    if (filter === 'all') {
      return all;
    }

    return typeof filter === 'string'
      ? all.filter((t) => t.kind === filter)
      : all.filter((t) => t.category === filter.category);
  });

  readonly categories = computed(() =>
    [...new Set(this.items().map((t) => t.category))].sort(),
  );

  setFilter(filter: TransactionFilter): void {
    this.activeFilter.set(filter);
  }

  load(): void {
    this.busy.set(true);

    this.api.list(this.months.month()).subscribe({
      next: (transactions) => {
        this.items.set(transactions);
        this.busy.set(false);
      },
      error: () => this.busy.set(false),
    });
  }

  create(transaction: CreateTransaction, onSuccess?: () => void): void {
    this.api.create(transaction).subscribe(() => {
      this.toasts.show('Lancamento registrado');
      this.load();
      onSuccess?.();
    });
  }

  remove(id: string): void {
    this.api.remove(id).subscribe(() => {
      this.toasts.show('Lancamento removido');
      this.load();
    });
  }
}

import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CategoryDefinition, CategoryScope } from '../../../shared/models/category';
import { TransactionKind } from '../../../shared/models/transaction';
import { CategoriesApi } from '../../settings/data-access/categories.api';
import { TransactionsStore } from '../data-access/transactions.store';

/** Cada tipo de lancamento so oferece as categorias que fazem sentido para ele. */
const SCOPE_BY_KIND: Record<TransactionKind, CategoryScope> = {
  Expense: 'Variable',
  Income: 'Income',
  FixedExpense: 'Fixed',
};

@Component({
  selector: 'app-transaction-form-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './transaction-form-page.html',
  styleUrl: './transaction-form-page.scss',
})
export class TransactionFormPage implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly categoriesApi = inject(CategoriesApi);
  private readonly store = inject(TransactionsStore);
  private readonly router = inject(Router);

  private readonly catalog = signal<readonly CategoryDefinition[]>([]);

  protected readonly kind = signal<TransactionKind>('Expense');

  protected readonly categories = computed(() =>
    this.catalog().filter((category) => category.scope === SCOPE_BY_KIND[this.kind()]),
  );

  protected readonly form = this.formBuilder.nonNullable.group({
    description: ['', [Validators.required, Validators.maxLength(120)]],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
    category: ['', Validators.required],
    occurredOn: [new Date().toISOString().slice(0, 10), Validators.required],
  });

  ngOnInit(): void {
    this.categoriesApi.catalog().subscribe((catalog) => {
      this.catalog.set(catalog);
      this.resetCategory();
    });
  }

  protected selectKind(kind: TransactionKind): void {
    this.kind.set(kind);
    this.resetCategory();
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    this.store.create(
      {
        kind: this.kind(),
        description: value.description,
        amount: Number(value.amount),
        category: value.category,
        occurredOn: value.occurredOn,
      },
      () => this.router.navigate(['/lancamentos']),
    );
  }

  private resetCategory(): void {
    this.form.controls.category.setValue(this.categories()[0]?.name ?? '');
  }
}

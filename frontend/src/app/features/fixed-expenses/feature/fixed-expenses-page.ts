import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MonthService } from '../../../shared/month.service';
import { CategoryDefinition } from '../../../shared/models/category';
import { FixedExpense } from '../../../shared/models/fixed-expense';
import { BrlPipe } from '../../../shared/ui/brl.pipe';
import { EmptyStateComponent } from '../../../shared/ui/empty-state';
import { ToastService } from '../../../shared/ui/toast.service';
import { CategoriesApi } from '../../settings/data-access/categories.api';
import { FixedExpensesApi } from '../data-access/fixed-expenses.api';

@Component({
  selector: 'app-fixed-expenses-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, BrlPipe, EmptyStateComponent],
  templateUrl: './fixed-expenses-page.html',
})
export class FixedExpensesPage implements OnInit {
  private readonly api = inject(FixedExpensesApi);
  private readonly categoriesApi = inject(CategoriesApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly months = inject(MonthService);
  private readonly toasts = inject(ToastService);

  private readonly items = signal<readonly FixedExpense[]>([]);

  protected readonly categories = signal<readonly CategoryDefinition[]>([]);
  protected readonly expenses = this.items.asReadonly();

  protected readonly total = computed(() =>
    this.items().reduce((sum, expense) => sum + expense.amount, 0),
  );

  protected readonly form = this.formBuilder.nonNullable.group({
    description: ['', [Validators.required, Validators.maxLength(120)]],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
    category: ['', Validators.required],
    dayOfMonth: [5, [Validators.required, Validators.min(1), Validators.max(31)]],
  });

  ngOnInit(): void {
    this.load();

    this.categoriesApi.catalog().subscribe((catalog) => {
      const fixed = catalog.filter((category) => category.scope === 'Fixed');
      this.categories.set(fixed);
      this.form.controls.category.setValue(fixed[0]?.name ?? '');
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    this.api
      .create({
        description: value.description,
        amount: Number(value.amount),
        category: value.category,
        dayOfMonth: value.dayOfMonth,
      })
      .subscribe(() => {
        this.toasts.show('Gasto fixo adicionado');
        this.form.controls.description.reset('');
        this.form.controls.amount.reset(null);
        this.load();
      });
  }

  protected deactivate(expense: FixedExpense): void {
    const confirmed = confirm(
      `Desativar "${expense.description}"? Os lancamentos ja gerados continuam no historico.`,
    );

    if (confirmed) {
      this.api.deactivate(expense.id).subscribe(() => {
        this.toasts.show('Gasto fixo desativado');
        this.load();
      });
    }
  }

  /** Gera os lancamentos do mes corrente para quem cadastrou fixos depois da virada. */
  protected materialize(): void {
    this.api.materialize(this.months.month()).subscribe((result) => {
      this.toasts.show(
        result.created > 0
          ? `${result.created} lancamento(s) gerado(s)`
          : 'Nenhum lancamento pendente neste mes',
      );
    });
  }

  private load(): void {
    this.api.list().subscribe((expenses) => this.items.set(expenses));
  }
}

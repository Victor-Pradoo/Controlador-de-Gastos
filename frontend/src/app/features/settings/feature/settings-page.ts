import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { MonthService } from '../../../core/competence/month.service';
import { CompetenceApi } from '../../../core/competence/competence.api';
import { MAX_CLOSING_DAY, MIN_CLOSING_DAY } from '../../../shared/models/competence';
import { BrlPipe } from '../../../shared/ui/brl.pipe';
import { ToastService } from '../../../shared/ui/toast.service';
import { BudgetApi } from '../../dashboard/data-access/budget.api';

@Component({
  selector: 'app-settings-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, BrlPipe, DatePipe],
  templateUrl: './settings-page.html',
})
export class SettingsPage implements OnInit {
  private readonly api = inject(BudgetApi);
  private readonly competenceApi = inject(CompetenceApi);
  private readonly months = inject(MonthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly toasts = inject(ToastService);

  protected readonly minClosingDay = MIN_CLOSING_DAY;
  protected readonly maxClosingDay = MAX_CLOSING_DAY;

  protected readonly form = this.formBuilder.nonNullable.group({
    salary: [0, [Validators.required, Validators.min(0)]],
    reserveRate: [20, [Validators.required, Validators.min(0), Validators.max(100)]],
  });

  protected readonly competenceForm = this.formBuilder.group({
    closingDay: [
      null as number | null,
      [Validators.min(MIN_CLOSING_DAY), Validators.max(MAX_CLOSING_DAY)],
    ],
  });

  private readonly competenceValue = toSignal(this.competenceForm.valueChanges, {
    initialValue: this.competenceForm.getRawValue(),
  });

  /** Vazio no formulario significa "sem virada", nao zero. */
  protected readonly closingDay = computed(() => this.competenceValue()?.closingDay || null);

  protected readonly currentWindow = computed(() => this.months.competence()?.currentWindow ?? null);

  /** So faz sentido salvar - e so faz sentido avisar - se o valor mudou de verdade. */
  protected readonly closingDayChanged = computed(
    () => this.closingDay() !== (this.months.competence()?.closingDay ?? null),
  );

  /** Preview ao vivo, como no app legado: o usuario ve o efeito antes de salvar. */
  private readonly value = toSignal(this.form.valueChanges, {
    initialValue: this.form.getRawValue(),
  });

  protected readonly reserveAmount = computed(() => {
    const { salary = 0, reserveRate = 0 } = this.value() ?? {};
    return (salary ?? 0) * ((reserveRate ?? 0) / 100);
  });

  protected readonly available = computed(() => (this.value()?.salary ?? 0) - this.reserveAmount());

  protected readonly devUserId = signal(localStorage.getItem('devUserId') ?? '');

  ngOnInit(): void {
    this.api.settings().subscribe((settings) => this.form.patchValue(settings));
    this.competenceForm.patchValue({ closingDay: this.months.competence()?.closingDay ?? null });
  }

  protected saveClosingDay(): void {
    if (this.competenceForm.invalid) {
      this.competenceForm.markAllAsTouched();
      return;
    }

    this.competenceApi.updateClosingDay(this.closingDay()).subscribe(() => {
      // A regra mudou: recarregar reposiciona o app na competencia corrente nova.
      this.months.refresh();
      this.toasts.show('Dia de virada atualizado');
    });
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.api.updateSettings(this.form.getRawValue()).subscribe(() => {
      this.toasts.show('Configuracao salva');
    });
  }

  /** TEMPORARIO: enquanto nao ha login, permite testar com mais de um usuario. */
  protected saveDevUser(value: string): void {
    const trimmed = value.trim();

    if (trimmed) {
      localStorage.setItem('devUserId', trimmed);
    } else {
      localStorage.removeItem('devUserId');
    }

    this.devUserId.set(trimmed);
    this.toasts.show('Usuario de desenvolvimento atualizado', 'info');
  }
}

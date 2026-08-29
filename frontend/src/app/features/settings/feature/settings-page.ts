import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { BrlPipe } from '../../../shared/ui/brl.pipe';
import { ToastService } from '../../../shared/ui/toast.service';
import { BudgetApi } from '../../dashboard/data-access/budget.api';

@Component({
  selector: 'app-settings-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, BrlPipe],
  templateUrl: './settings-page.html',
})
export class SettingsPage implements OnInit {
  private readonly api = inject(BudgetApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly toasts = inject(ToastService);

  protected readonly form = this.formBuilder.nonNullable.group({
    salary: [0, [Validators.required, Validators.min(0)]],
    reserveRate: [20, [Validators.required, Validators.min(0), Validators.max(100)]],
  });

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

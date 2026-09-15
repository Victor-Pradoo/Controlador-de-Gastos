import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { CompetenceApi } from '../../../core/competence/competence.api';
import { MonthService } from '../../../core/competence/month.service';
import { BrlPipe } from '../../../shared/ui/brl.pipe';
import { EmptyStateComponent } from '../../../shared/ui/empty-state';
import { ToastService } from '../../../shared/ui/toast.service';
import { DashboardStore } from '../data-access/dashboard.store';

/** Tela inicial: quanto sobra, para onde foi e o quao apertado esta o mes. */
@Component({
  selector: 'app-dashboard-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BrlPipe, DatePipe, EmptyStateComponent],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss',
})
export class DashboardPage {
  private readonly competenceApi = inject(CompetenceApi);
  private readonly toasts = inject(ToastService);

  protected readonly months = inject(MonthService);
  protected readonly store = inject(DashboardStore);

  protected readonly closing = signal(false);

  /** Cor do saldo e da barra acompanham o semaforo calculado no backend. */
  protected readonly healthClass = computed(() => {
    const health = this.store.budget()?.health;
    return health === 'Critical' ? 'is-bad' : health === 'Warning' ? 'is-warn' : 'is-good';
  });

  constructor() {
    // Trocar o mes na topbar recarrega a tela sozinho.
    effect(() => {
      this.months.month();
      this.store.load();
    });
  }

  /**
   * Encerra a competencia corrente: a partir de hoje, gasto novo entra na seguinte.
   * O que ja foi lancado com data anterior fica onde esta.
   */
  protected closeCompetence(): void {
    if (!confirm('Encerrar os gastos deste mes? Os novos lancamentos passam a entrar na competencia seguinte.')) {
      return;
    }

    this.closing.set(true);

    this.competenceApi.close().subscribe({
      next: (result) => {
        this.months.refresh();
        this.closing.set(false);
        this.toasts.show(`Competencia encerrada. Novos gastos entram em ${result.current}.`);
      },
      error: () => this.closing.set(false),
    });
  }

  protected reopenCompetence(competence: string): void {
    this.closing.set(true);

    this.competenceApi.reopen(competence).subscribe({
      next: () => {
        this.months.refresh();
        this.closing.set(false);
        this.toasts.show('Competencia reaberta');
      },
      error: () => this.closing.set(false),
    });
  }
}

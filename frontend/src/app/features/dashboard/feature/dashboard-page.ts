import { ChangeDetectionStrategy, Component, computed, effect, inject } from '@angular/core';
import { MonthService } from '../../../shared/month.service';
import { BrlPipe } from '../../../shared/ui/brl.pipe';
import { EmptyStateComponent } from '../../../shared/ui/empty-state';
import { DashboardStore } from '../data-access/dashboard.store';

/** Tela inicial: quanto sobra, para onde foi e o quao apertado esta o mes. */
@Component({
  selector: 'app-dashboard-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BrlPipe, EmptyStateComponent],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss',
})
export class DashboardPage {
  private readonly months = inject(MonthService);

  protected readonly store = inject(DashboardStore);

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
}

import { Injectable, computed, inject, signal } from '@angular/core';
import { catchError, of, tap } from 'rxjs';
import { CompetenceSettings } from '../../shared/models/competence';
import { CompetenceApi } from './competence.api';

const MONTH_LABELS = [
  'Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun',
  'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez',
];

/**
 * Competencia selecionada, compartilhada por todas as features.
 *
 * Vive em `core/` e nao em `shared/`: e um singleton de app inteiro e faz I/O, porque
 * a competencia corrente nao e mais o mes do calendario - depende da regra de virada
 * do usuario, que so o backend conhece. Ate a regra chegar (ou se a chamada falhar),
 * o mes do calendario e um palpite razoavel e a aplicacao sobe do mesmo jeito.
 */
@Injectable({ providedIn: 'root' })
export class MonthService {
  private readonly api = inject(CompetenceApi);

  static calendarMonth(today = new Date()): string {
    return `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}`;
  }

  private readonly selected = signal(MonthService.calendarMonth());
  private readonly settings = signal<CompetenceSettings | null>(null);

  readonly month = this.selected.asReadonly();

  /** A regra vigente. null enquanto nao carregou ou se o backend nao respondeu. */
  readonly competence = this.settings.asReadonly();

  readonly label = computed(() => {
    const [year, month] = this.selected().split('-').map(Number);
    return `${MONTH_LABELS[month - 1]} ${year}`;
  });

  /** A competencia em foco e a corrente segundo a regra? */
  readonly isCurrent = computed(() => this.selected() === this.settings()?.current);

  /** Ha um encerramento manual para desfazer, e estamos vendo a competencia que ele abriu. */
  readonly reopenable = computed(() => {
    const settings = this.settings();
    return settings?.reopenableCompetence && this.isCurrent() ? settings.reopenableCompetence : null;
  });

  /** Janela de datas da competencia corrente, quando ela e a que esta em foco. */
  readonly currentWindow = computed(() =>
    this.isCurrent() ? (this.settings()?.currentWindow ?? null) : null,
  );

  /**
   * Carrega a regra e posiciona na competencia corrente. Chamado no bootstrap para
   * nenhuma tela abrir num mes e pular para outro logo depois.
   */
  load() {
    return this.api.settings().pipe(
      tap((settings) => {
        this.settings.set(settings);
        this.selected.set(settings.current);
      }),
      catchError(() => of(null)),
    );
  }

  /** Reaplica a regra depois de ela mudar (dia de virada, encerramento, reabertura). */
  refresh(): void {
    this.load().subscribe();
  }

  select(month: string): void {
    this.selected.set(month);
  }

  shift(months: number): void {
    const [year, month] = this.selected().split('-').map(Number);
    const date = new Date(year, month - 1 + months, 1);
    this.selected.set(`${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`);
  }
}

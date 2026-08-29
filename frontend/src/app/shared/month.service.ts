import { Injectable, computed, signal } from '@angular/core';

const MONTH_LABELS = [
  'Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun',
  'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez',
];

/**
 * Competencia selecionada, compartilhada por todas as features.
 * O app inteiro raciocina por mes; manter isso num unico signal evita
 * cada tela ter a sua propria nocao de "mes atual".
 */
@Injectable({ providedIn: 'root' })
export class MonthService {
  private static current(): string {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
  }

  private readonly selected = signal(MonthService.current());

  readonly month = this.selected.asReadonly();

  readonly label = computed(() => {
    const [year, month] = this.selected().split('-').map(Number);
    return `${MONTH_LABELS[month - 1]} ${year}`;
  });

  select(month: string): void {
    this.selected.set(month);
  }

  shift(months: number): void {
    const [year, month] = this.selected().split('-').map(Number);
    const date = new Date(year, month - 1 + months, 1);
    this.selected.set(`${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`);
  }
}

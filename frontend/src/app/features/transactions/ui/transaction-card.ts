import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Transaction } from '../../../shared/models/transaction';
import { BrlPipe } from '../../../shared/ui/brl.pipe';

/** Componente burro: recebe o lancamento, emite a intencao de remover. */
@Component({
  selector: 'app-transaction-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, BrlPipe],
  template: `
    <article class="card row" [class.row--income]="isIncome()" [class.row--fixed]="isFixed()">
      <span class="dot"></span>

      <div class="row__info">
        <p class="row__name">{{ transaction().description }}</p>
        <p class="row__sub mono">
          {{ transaction().category }} · {{ transaction().occurredOn | date: 'dd/MM' }}
          @if (badge(); as text) {
            <span class="badge">{{ text }}</span>
          }
        </p>
      </div>

      <div class="row__right">
        <span class="row__value" [class.row__value--income]="isIncome()">
          {{ isIncome() ? '+' : '-' }} {{ transaction().amount | brl }}
        </span>

        @if (transaction().isEditable) {
          <button type="button" class="row__delete" aria-label="Remover" (click)="remove.emit(transaction().id)">
            &times;
          </button>
        }
      </div>
    </article>
  `,
  styles: `
    .row {
      display: flex;
      align-items: center;
      gap: 11px;
    }
    .row--income { border-left: 3px solid var(--income); }
    .row--fixed { border-left: 3px solid var(--fixed); }
    .dot {
      flex-shrink: 0;
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: var(--accent);
    }
    .row--income .dot { background: var(--income); }
    .row--fixed .dot { background: var(--fixed); }
    .row__info { flex: 1; min-width: 0; }
    .row__name { font-size: 13px; font-weight: 600; }
    .row__sub { margin-top: 3px; font-size: 11px; color: var(--muted); }
    .badge {
      margin-left: 6px;
      padding: 1px 6px;
      border-radius: 99px;
      background: var(--surface2);
      font-size: 9px;
      text-transform: uppercase;
    }
    .row__right { display: flex; align-items: center; gap: 8px; }
    .row__value { font-family: var(--font-mono); font-size: 12px; }
    .row__value--income { color: var(--income); }
    .row__delete {
      width: 26px;
      height: 26px;
      border: 1px solid var(--border);
      border-radius: 50%;
      background: transparent;
      color: var(--muted);
      font-size: 16px;
      line-height: 1;
      cursor: pointer;
    }
  `,
})
export class TransactionCardComponent {
  readonly transaction = input.required<Transaction>();

  readonly remove = output<string>();

  protected readonly isIncome = computed(() => this.transaction().kind === 'Income');

  protected readonly isFixed = computed(() => this.transaction().kind === 'FixedExpense');

  protected readonly badge = computed(() =>
    this.isFixed() ? 'fixo' : this.transaction().source === 'BankSync' ? 'banco' : null,
  );
}

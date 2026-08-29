import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div class="empty">{{ message() }}</div>`,
  styles: `
    .empty {
      padding: 32px 20px;
      text-align: center;
      color: var(--muted);
      font-family: var(--font-mono);
      font-size: 13px;
    }
  `,
})
export class EmptyStateComponent {
  readonly message = input.required<string>();
}

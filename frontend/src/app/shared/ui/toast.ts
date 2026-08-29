import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from './toast.service';

@Component({
  selector: 'app-toast',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (toasts.toast(); as toast) {
      <div class="toast" [class]="'toast--' + toast.kind">{{ toast.message }}</div>
    }
  `,
  styles: `
    .toast {
      position: fixed;
      bottom: 96px;
      left: 50%;
      transform: translateX(-50%);
      z-index: 200;
      max-width: 90vw;
      padding: 12px 18px;
      border-radius: var(--radius-sm);
      background: var(--surface2);
      border: 1px solid var(--border);
      color: var(--text);
      font-family: var(--font-mono);
      font-size: 13px;
    }
    .toast--success { border-color: var(--accent); color: var(--accent); }
    .toast--error { border-color: var(--danger); color: var(--danger); }
  `,
})
export class ToastComponent {
  protected readonly toasts = inject(ToastService);
}

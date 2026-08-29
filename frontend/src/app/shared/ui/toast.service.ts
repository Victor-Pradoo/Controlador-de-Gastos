import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info';

export interface Toast {
  readonly message: string;
  readonly kind: ToastKind;
}

/** Mesmo feedback do app legado, agora como estado reativo em vez de innerHTML. */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly current = signal<Toast | null>(null);
  private timer?: ReturnType<typeof setTimeout>;

  readonly toast = this.current.asReadonly();

  show(message: string, kind: ToastKind = 'success'): void {
    this.current.set({ message, kind });

    clearTimeout(this.timer);
    this.timer = setTimeout(() => this.current.set(null), 2500);
  }
}

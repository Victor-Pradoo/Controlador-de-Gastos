import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { BankConnection, BankConnectionStatus } from '../../../shared/models/bank-connection';
import { EmptyStateComponent } from '../../../shared/ui/empty-state';
import { ToastService } from '../../../shared/ui/toast.service';
import { BankingApi } from '../data-access/banking.api';

const STATUS_LABEL: Record<BankConnectionStatus, string> = {
  Pending: 'aguardando primeira sincronizacao',
  Active: 'conectado',
  RequiresAction: 'precisa da sua acao no banco',
  Error: 'erro na ultima sincronizacao',
  Disabled: 'desativada',
};

/**
 * Conexoes de Open Finance - a feature que define este MVP.
 *
 * O fluxo real e: pedir um connect token -> abrir o widget do provedor (SDK do
 * front) -> o widget devolve um itemId -> registrar esse itemId aqui. Enquanto o
 * SDK nao esta plugado, o itemId pode ser colado a mao, o que ja funciona contra
 * o provedor falso do backend.
 */
@Component({
  selector: 'app-bank-connections-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, ReactiveFormsModule, EmptyStateComponent],
  styles: `.connect-form { margin-top: 16px; }`,
  templateUrl: './bank-connections-page.html',
})
export class BankConnectionsPage implements OnInit {
  private readonly api = inject(BankingApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly toasts = inject(ToastService);

  protected readonly connections = signal<readonly BankConnection[]>([]);
  protected readonly syncing = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    itemId: ['', Validators.required],
  });

  ngOnInit(): void {
    this.load();
  }

  protected statusLabel(status: BankConnectionStatus): string {
    return STATUS_LABEL[status];
  }

  /**
   * TODO: substituir por `new PluggyConnect({ connectToken })` do SDK oficial
   * assim que as credenciais de sandbox estiverem configuradas.
   */
  protected requestConnectToken(): void {
    this.api.connectToken().subscribe(({ token }) => {
      this.toasts.show('Connect token gerado. Use-o no widget do provedor.', 'info');
      console.info('connect token:', token);
    });
  }

  protected connect(): void {
    if (this.form.invalid) {
      return;
    }

    this.api.connect(this.form.getRawValue().itemId).subscribe(() => {
      this.toasts.show('Banco conectado');
      this.form.reset({ itemId: '' });
      this.load();
    });
  }

  protected sync(connection: BankConnection): void {
    this.syncing.set(connection.id);

    this.api.sync(connection.id).subscribe({
      next: (result) => {
        this.syncing.set(null);
        this.toasts.show(
          `${result.imported} importada(s), ${result.skipped} ja existente(s)`,
          result.failed > 0 ? 'error' : 'success',
        );
        this.load();
      },
      error: () => this.syncing.set(null),
    });
  }

  private load(): void {
    this.api.list().subscribe((connections) => this.connections.set(connections));
  }
}

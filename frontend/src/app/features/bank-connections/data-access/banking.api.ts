import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/api/api.tokens';
import { BankConnection, BankSyncResult } from '../../../shared/models/bank-connection';

@Injectable({ providedIn: 'root' })
export class BankingApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/banking`;

  list(): Observable<BankConnection[]> {
    return this.http.get<BankConnection[]>(`${this.baseUrl}/connections`);
  }

  /** Token de curta duracao para abrir o widget do provedor. */
  connectToken(): Observable<{ token: string }> {
    return this.http.post<{ token: string }>(`${this.baseUrl}/connect-token`, {});
  }

  /** Registra o itemId que o widget devolve ao concluir a conexao. */
  connect(itemId: string): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.baseUrl}/connections`, { itemId });
  }

  sync(id: string, daysBack = 30): Observable<BankSyncResult> {
    return this.http.post<BankSyncResult>(
      `${this.baseUrl}/connections/${id}/sync`,
      {},
      { params: { daysBack } },
    );
  }
}

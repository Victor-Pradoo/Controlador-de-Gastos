import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/api/api.tokens';
import { CreateTransaction, LedgerSummary, Transaction } from '../../../shared/models/transaction';

/** Acesso HTTP ao modulo Ledger. Nenhum componente chama HttpClient direto. */
@Injectable({ providedIn: 'root' })
export class TransactionsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/ledger`;

  list(month: string): Observable<Transaction[]> {
    return this.http.get<Transaction[]>(`${this.baseUrl}/transactions`, { params: { month } });
  }

  summary(month: string): Observable<LedgerSummary> {
    return this.http.get<LedgerSummary>(`${this.baseUrl}/summary`, { params: { month } });
  }

  create(transaction: CreateTransaction): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.baseUrl}/transactions`, transaction);
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/transactions/${id}`);
  }
}

import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/api/api.tokens';
import { CreateFixedExpense, FixedExpense } from '../../../shared/models/fixed-expense';

@Injectable({ providedIn: 'root' })
export class FixedExpensesApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/fixed-expenses`;

  list(): Observable<FixedExpense[]> {
    return this.http.get<FixedExpense[]>(this.baseUrl);
  }

  create(expense: CreateFixedExpense): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.baseUrl, expense);
  }

  deactivate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /** Gera os lancamentos do mes. Idempotente no backend. */
  materialize(month: string): Observable<{ month: string; created: number }> {
    return this.http.post<{ month: string; created: number }>(
      `${this.baseUrl}/materialize`,
      {},
      { params: { month } },
    );
  }
}

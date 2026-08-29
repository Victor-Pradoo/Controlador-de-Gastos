import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/api/api.tokens';
import { BudgetSettings, MonthlyBudget } from '../../../shared/models/budget';

@Injectable({ providedIn: 'root' })
export class BudgetApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/budget`;

  monthly(month: string): Observable<MonthlyBudget> {
    return this.http.get<MonthlyBudget>(this.baseUrl, { params: { month } });
  }

  settings(): Observable<BudgetSettings> {
    return this.http.get<BudgetSettings>(`${this.baseUrl}/settings`);
  }

  updateSettings(settings: BudgetSettings): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/settings`, settings);
  }
}

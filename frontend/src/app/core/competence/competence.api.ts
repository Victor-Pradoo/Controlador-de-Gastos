import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api/api.tokens';
import { CloseCompetenceResult, CompetenceSettings } from '../../shared/models/competence';

/** Acesso HTTP a regra de virada de competencia, que vive no modulo Ledger. */
@Injectable({ providedIn: 'root' })
export class CompetenceApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/ledger/competence`;

  settings(): Observable<CompetenceSettings> {
    return this.http.get<CompetenceSettings>(this.baseUrl);
  }

  updateClosingDay(closingDay: number | null): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/closing-day`, { closingDay });
  }

  close(): Observable<CloseCompetenceResult> {
    return this.http.post<CloseCompetenceResult>(`${this.baseUrl}/close`, {});
  }

  reopen(competence: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/close/${competence}`);
  }
}

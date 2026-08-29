import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/api/api.tokens';
import { CategoryDefinition } from '../../../shared/models/category';

@Injectable({ providedIn: 'root' })
export class CategoriesApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/categories`;

  /** Catalogo vem do backend: a lista de categorias nao vive duplicada aqui. */
  catalog(): Observable<CategoryDefinition[]> {
    return this.http.get<CategoryDefinition[]>(this.baseUrl);
  }
}

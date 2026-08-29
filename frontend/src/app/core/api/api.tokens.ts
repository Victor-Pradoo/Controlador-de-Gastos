import { InjectionToken } from '@angular/core';
import { environment } from '../../../environments/environment';

/**
 * Base da API. Injetada em vez de importada direto para os testes poderem
 * trocar por um stub sem tocar em `environment`.
 */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => environment.apiBaseUrl,
});

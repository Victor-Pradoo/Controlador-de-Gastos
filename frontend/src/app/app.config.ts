import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding, withViewTransitions } from '@angular/router';
import { devUserInterceptor } from './core/api/dev-user.interceptor';
import { httpErrorInterceptor } from './core/api/http-error.interceptor';
import { MonthService } from './core/competence/month.service';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding(), withViewTransitions()),
    provideHttpClient(withInterceptors([devUserInterceptor, httpErrorInterceptor])),
    // A competencia corrente sai da regra de virada do usuario, nao do calendario.
    // Carregar antes de abrir a primeira tela evita ela renderizar num mes e pular
    // para outro. Se falhar, o MonthService cai no mes do calendario e o app sobe.
    provideAppInitializer(() => inject(MonthService).load()),
  ],
};

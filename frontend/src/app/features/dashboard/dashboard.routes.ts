import { Routes } from '@angular/router';

/**
 * Rotas da feature. Carregadas sob demanda pelo app.routes.ts:
 * cada feature vira um chunk proprio.
 */
export const dashboardRoutes: Routes = [
  {
    path: '',
    title: 'Meus Gastos',
    loadComponent: () => import('./feature/dashboard-page').then((m) => m.DashboardPage),
  },
];

import { Routes } from '@angular/router';

/**
 * Rotas raiz. Cada feature entra por lazy loading do seu proprio arquivo de rotas:
 * a feature e a unidade de organizacao E a de carregamento.
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'inicio' },
  {
    path: 'inicio',
    loadChildren: () => import('./features/dashboard/dashboard.routes').then((m) => m.dashboardRoutes),
  },
  {
    path: 'lancamentos',
    loadChildren: () =>
      import('./features/transactions/transactions.routes').then((m) => m.transactionsRoutes),
  },
  {
    path: 'fixos',
    loadChildren: () =>
      import('./features/fixed-expenses/fixed-expenses.routes').then((m) => m.fixedExpensesRoutes),
  },
  {
    path: 'bancos',
    loadChildren: () =>
      import('./features/bank-connections/bank-connections.routes').then(
        (m) => m.bankConnectionsRoutes,
      ),
  },
  {
    path: 'ajustes',
    loadChildren: () => import('./features/settings/settings.routes').then((m) => m.settingsRoutes),
  },
  { path: '**', redirectTo: 'inicio' },
];

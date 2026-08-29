import { Routes } from '@angular/router';

export const fixedExpensesRoutes: Routes = [
  {
    path: '',
    title: 'Gastos fixos',
    loadComponent: () => import('./feature/fixed-expenses-page').then((m) => m.FixedExpensesPage),
  },
];

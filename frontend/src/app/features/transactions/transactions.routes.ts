import { Routes } from '@angular/router';

export const transactionsRoutes: Routes = [
  {
    path: '',
    title: 'Lancamentos',
    loadComponent: () => import('./feature/transactions-page').then((m) => m.TransactionsPage),
  },
  {
    path: 'novo',
    title: 'Novo lancamento',
    loadComponent: () =>
      import('./feature/transaction-form-page').then((m) => m.TransactionFormPage),
  },
];

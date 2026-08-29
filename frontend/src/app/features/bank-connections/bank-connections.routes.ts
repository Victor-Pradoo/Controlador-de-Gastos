import { Routes } from '@angular/router';

export const bankConnectionsRoutes: Routes = [
  {
    path: '',
    title: 'Bancos',
    loadComponent: () =>
      import('./feature/bank-connections-page').then((m) => m.BankConnectionsPage),
  },
];

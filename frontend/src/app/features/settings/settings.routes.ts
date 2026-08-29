import { Routes } from '@angular/router';

export const settingsRoutes: Routes = [
  {
    path: '',
    title: 'Ajustes',
    loadComponent: () => import('./feature/settings-page').then((m) => m.SettingsPage),
  },
];

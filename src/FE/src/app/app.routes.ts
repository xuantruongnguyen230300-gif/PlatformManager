import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'dashboard',
    loadChildren: () => import('./modules/dashboard/dashboard.routes').then((m) => m.DASHBOARD_ROUTES)
  },
  {
    path: 'danh-muc/dti',
    loadChildren: () => import('./modules/danh-muc-dti/danh-muc-dti.routes').then((m) => m.DANH_MUC_DTI_ROUTES)
  },
  { path: '**', redirectTo: 'dashboard' }
];

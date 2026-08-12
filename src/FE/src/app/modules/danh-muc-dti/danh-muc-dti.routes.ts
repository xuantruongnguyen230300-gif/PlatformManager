import { Routes } from '@angular/router';

export const DANH_MUC_DTI_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/danh-muc-dti/danh-muc-dti.page').then((m) => m.DanhMucDtiPage)
  }
];

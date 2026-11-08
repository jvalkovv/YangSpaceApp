import { Routes } from '@angular/router';
import { HomeComponent } from './home.component';
import { DetailComponent } from './detail.component';
import { AdminComponent } from './admin.component';

export const routes: Routes = [
  { path: '', component: HomeComponent, title: 'YangSpace — кварталът около теб' },
  { path: 'obekt/:slug', component: DetailComponent, title: 'Обект | YangSpace' },
  { path: 'admin', component: AdminComponent, title: 'Preview управление | YangSpace' },
  { path: '**', redirectTo: '' }
];

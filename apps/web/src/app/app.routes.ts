import { Routes } from '@angular/router';
import { HomeComponent } from './home.component';
import { DetailComponent } from './detail.component';
import { AdminComponent } from './admin.component';
import { AccountComponent } from './account.component';

export const routes: Routes = [
  { path: '', component: HomeComponent, title: 'YangSpace — кварталът около теб' },
  { path: 'obekt/:slug', component: DetailComponent, title: 'Обект | YangSpace' },
  { path: 'admin', component: AdminComponent, title: 'Preview управление | YangSpace' },
  { path: 'login', component: AccountComponent, data: { mode: 'login' }, title: 'Вход | YangSpace' },
  { path: 'register', component: AccountComponent, data: { mode: 'register' }, title: 'Регистрация | YangSpace' },
  { path: 'recover', component: AccountComponent, data: { mode: 'recover' }, title: 'Забравена парола | YangSpace' },
  { path: 'reset', component: AccountComponent, data: { mode: 'reset' }, title: 'Нова парола | YangSpace' },
  { path: 'resend-confirmation', component: AccountComponent, data: { mode: 'resend' }, title: 'Потвърди имейла | YangSpace' },
  { path: 'account', component: AccountComponent, data: { mode: 'account' }, title: 'Моят профил | YangSpace' },
  { path: '**', redirectTo: '' }
];

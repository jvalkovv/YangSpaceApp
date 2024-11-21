import { Routes } from '@angular/router';
import { HomepageComponent } from './home/homepage.component';
import { LoginComponent } from './auth/login/login.component';
import { RegisterComponent } from './auth/register/register.component';
import { UserProfileComponent } from './user-profile/user-profile.component';
import { AuthGuard } from './auth.guard';
import { AuthReverseGuard } from './auth-reverse.guard';


export const routes: Routes = [
  { path: '', redirectTo: '/homepage', pathMatch: 'full' }, // Default redirect
  { path: 'homepage', component: HomepageComponent },       // Home route
  { path: 'register', component: RegisterComponent, canActivate: [AuthReverseGuard] }, // Allow only unauthenticated users
  { path: 'login', component: LoginComponent, canActivate: [AuthReverseGuard] }, // Redirect logged-in users away from login
  { path: 'user-profile', component: UserProfileComponent, canActivate: [AuthGuard] }, // Protect profile for authenticated users

];
export class AppRoutingModule { }
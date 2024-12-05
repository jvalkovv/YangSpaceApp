import { Routes } from '@angular/router';
import { HomepageComponent } from './home/homepage.component';
import { UserProfileComponent } from './user-profile/user-profile.component';
import { AuthGuard } from './auth/guards/auth.guard';
import { AuthReverseGuard } from './auth/guards/auth-reverse.guard';
import { RegisterComponent } from './auth/components/register/register.component';
import { LoginComponent } from './auth/components/login/login.component';
import { EditProfileComponent } from './edit-profile/edit-profile.component';
import { BookedServicesComponent } from './booked-services/booked-services.component';
import { AllServicesComponent } from './all-services/all-services.component';
import { CreateServiceComponent } from './create-service/create-service.component';
import { ServiceProviderGuard } from './create-service/service-provider.guard';
import { EditServiceComponent } from './edit-service/edit-service.component';
import { ServiceItemComponent } from './service-item/service-item.component';


export const routes: Routes = [
  { path: '', redirectTo: '/homepage', pathMatch: 'full' }, // Default redirect
  { path: 'homepage', component: HomepageComponent },       // Home route
  { path: 'all-services', component: AllServicesComponent },
  { path: 'create-service', component: CreateServiceComponent, canActivate: [ServiceProviderGuard]},
  { path: 'edit-service/:id', component: EditServiceComponent },
  { path: 'register', component: RegisterComponent, canActivate: [AuthReverseGuard] }, // Allow only unauthenticated users
  { path: 'login', component: LoginComponent, canActivate: [AuthReverseGuard] }, // Redirect logged-in users away from login
  { path: 'user-profile', component: UserProfileComponent, canActivate: [AuthGuard] }, // Protect profile for authenticated users
  { path: 'edit-profile', component: EditProfileComponent, canActivate: [AuthGuard] }, // Protect profile for authenticated users
  { path: 'booked-services', component: BookedServicesComponent, canActivate: [AuthGuard] }, // Protect profile for authenticated users
  { path: 'service-item', component: ServiceItemComponent, canActivate: [AuthGuard] },
 
];
export class AppRoutingModule { }
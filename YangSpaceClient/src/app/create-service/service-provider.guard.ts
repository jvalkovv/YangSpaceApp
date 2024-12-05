import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { AuthService } from '../auth/services/auth-service';

@Injectable({
  providedIn: 'root',
})
export class ServiceProviderGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(): boolean {
    // Use the observable and check if the user has the ServiceProvider role
    const userRole = this.authService.isServiceProvider();  // Check role, assuming this is a method in AuthService

    if (userRole) {
      return true;  // Allow access if the user is a ServiceProvider
    } else {
      this.router.navigate(['/login']);  // Redirect to login if not a ServiceProvider
      return false;
    }
  }
}

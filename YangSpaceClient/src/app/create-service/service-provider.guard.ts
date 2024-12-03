import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { AuthService } from '../auth/services/auth-service';

@Injectable({
  providedIn: 'root',
})
export class ServiceProviderGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(): boolean {
    const userRole = this.authService.isServiceProvider();
    console.log(userRole);
    
    
    if (userRole) {
      return true; 
    } else {
      this.router.navigate(['/login']); 
      return false;
    }
  }
}

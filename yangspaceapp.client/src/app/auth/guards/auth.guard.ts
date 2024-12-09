import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { AuthService } from '../services/auth-service';
@Injectable({
  providedIn: 'root',
})
export class AuthGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) { }

  canActivate(): boolean {
    let isLoggedIn = false;
    // Convert observable to a synchronous value
    this.authService.isLoggedIn$.subscribe((loggedIn) => {
      isLoggedIn = loggedIn;
    });

    if (!isLoggedIn) {
      this.router.navigate(['/login']);
      return false;
    }
    return true;

  }
}


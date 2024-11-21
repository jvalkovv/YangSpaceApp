import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { AuthService } from './auth/auth-service';

@Injectable({
  providedIn: 'root',
})
export class AuthReverseGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(): Observable<boolean> {
    return this.authService.isLoggedIn$.pipe(
      take(1),
      map((isLoggedIn) => {
        if (isLoggedIn) {
          this.router.navigate(['/user-profile']); // Redirect if already logged in
          return false; // Deny access to unauthenticated routes
        }
        return true; // Allow access to unauthenticated routes
      })
    );
  }
}

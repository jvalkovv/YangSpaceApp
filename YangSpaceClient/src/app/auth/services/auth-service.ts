import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { catchError, debounceTime, finalize, map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { UserLoginModel, UserRegistrationModel } from '../../models/user.model';
import { AbstractControl, AsyncValidatorFn, ValidationErrors } from '@angular/forms';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = environment.apiUrl;
  private tokenKey = environment.tokenKey;
  private usernameKey = environment.usernameKey;
  private maxInactivityTime = 15 * 60 * 1000; // 15 minutes

  // Authentication state
  private isLoggedInSubject = new BehaviorSubject<boolean>(this.hasValidToken());
  isLoggedIn$ = this.isLoggedInSubject.asObservable();

  isUsernameChecking = false;
  isEmailChecking = false;

  constructor(private http: HttpClient, private router: Router) {
    this.setupInactivityTimer();

    window.onbeforeunload = () => {
      this.logout();
    };
  }

  // Token Validation Methods
  hasValidToken(): boolean {
    const token = this.getToken();
    return !!token && !this.isTokenExpired(token);
  }

  private isTokenExpired(token: string): boolean {
    try {
      const decoded = this.decodeToken(token);
      return decoded.exp < Date.now() / 1000;
    } catch {
      return true;
    }
  }

  private decodeToken(token: string): any {
    try {
      return JSON.parse(atob(token.split('.')[1]));
    } catch {
      return null;
    }
  }

  // Token and User Management
  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getUsername(): string | null {
    return localStorage.getItem(this.usernameKey);
  }

  saveUserDetails(token: string, username: string): void {
    localStorage.setItem(this.tokenKey, token);
    localStorage.setItem(this.usernameKey, username);
    sessionStorage.setItem('lastActivity', Date.now().toString()); // Track last activity
    this.isLoggedInSubject.next(true);
    this.resetInactivityTimer();
  }

  // Authentication Methods
  login(credentials: UserLoginModel): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/account/login`, credentials).pipe(
      tap(response => {
        this.saveUserDetails(response.token, response.username);
      }),
      catchError(this.handleError)
    );
  }

  register(userData: UserRegistrationModel): Observable<any> {
    return this.http.post(`${this.apiUrl}/account/register`, userData).pipe(
      catchError(this.handleError)
    );
  }
  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.usernameKey);
    sessionStorage.removeItem('lastActivity');
    this.isLoggedInSubject.next(false);
    this.router.navigate(['/login']);
  }

  // Inactivity Timer
  setupInactivityTimer(): void {
    const inactivityCheck = () => {
      const lastActivity = sessionStorage.getItem('lastActivity');
      if (lastActivity && Date.now() - parseInt(lastActivity) > this.maxInactivityTime) {
        this.logout(); // Log out if inactive for more than maxInactivityTime
      }
    };

    // Check inactivity every minute
    setInterval(inactivityCheck, 60000);
  }
  resetInactivityTimer(): void {
    sessionStorage.setItem('lastActivity', Date.now().toString()); // Reset last activity timestamp
  }
  // Username Availability Check
  checkUsername(username: string): Observable<{ isUsernameTaken: boolean }> {
    return this.http.get<{ isUsernameTaken: boolean }>(
      `${this.apiUrl}/account/check-username/${username}`
    ).pipe(
      catchError(this.handleError)
    );
  }
  // Track activity
  trackUserActivity(): void {
    const events = ['mousemove', 'keydown', 'scroll', 'click'];

    events.forEach(event => {
      window.addEventListener(event, () => {
        this.resetInactivityTimer(); // Reset inactivity timer on activity
      });
    });
  }
  // Email Availability Check
  checkEmail(email: string): Observable<{ isEmailTaken: boolean }> {
    return this.http.get<{ isEmailTaken: boolean }>(
      `${this.apiUrl}/account/check-email/${email}`
    ).pipe(
      catchError(this.handleError)
    );
  }

  // Check if the user is authenticated
  isAuthenticated(): boolean {
    const token = localStorage.getItem(this.tokenKey);
    return !!token && !this.isTokenExpired(token);
  }

  // Check if the user has the 'ServiceProvider' role
  isServiceProvider(): boolean {
    const token = this.getToken();
    if (!token) {
      return false;
    }

    try {
      const decodedToken = JSON.parse(atob(token.split('.')[1]));
      return decodedToken === 'ServiceProvider';
    } catch (error) {
     
      return false;
    }
  }

  // Error Handling
  private handleError(error: HttpErrorResponse) {
    let errorMessage = 'An unknown error occurred!';

    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Error: ${error.error.message}`;
    } else {
      // Server-side error
      if (error.status === 401) {
        if (error.error.message === "Invalid username.") {
          errorMessage = 'Invalid username. Please check your username again.';
        } else if (error.error.message === "Invalid credentials(password).") {
          errorMessage = 'Invalid password. Please check your password again.';
        } else {
          errorMessage = 'Unauthorized. Please log in again.';
        }
      } else if (error.status === 403) {
        errorMessage = 'You do not have permission to access this resource.';
      } else {
        errorMessage = `Error Code: `;
      }
    }

    return throwError(() => new Error(errorMessage));
  }

  // Username Validator
  usernameValidator(): AsyncValidatorFn {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      if (!control.value) {
        return of(null);  // If no value is provided, no validation is needed.
      }

      this.isUsernameChecking = true;

      return this.checkUsername(control.value).pipe(
        debounceTime(500),  // Debounce for 500ms to wait before making the API call.
        map((response) => {
          return response.isUsernameTaken ? { usernameTaken: true } : null;  // If the username is taken, return error.
        }),
        catchError((error) => {
          console.error('Error during username validation:', error);
          return of({ usernameTaken: false });  // Return false for usernameTaken if error occurs.
        }),
        finalize(() => {
          this.isUsernameChecking = false;  // Reset the checking flag after API call.
        })
      );
    };
  }

  // Email Validator
  emailValidator(): AsyncValidatorFn {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      if (!control.value) {
        return of(null);  // If no value is provided, no validation is needed.
      }

      this.isEmailChecking = true;

      return this.checkEmail(control.value).pipe(
        debounceTime(500),  // Debounce for 500ms to wait before making the API call.
        map((response) => {
          return response.isEmailTaken ? { emailTaken: true } : null;  // If the email is taken, return error.
        }),
        catchError((error) => {
          console.error('Error during email validation:', error);
          return of({ emailTaken: false });  // Return false for emailTaken if error occurs.
        }),
        finalize(() => {
          this.isEmailChecking = false;  // Reset the checking flag after API call.
        })
      );
    };
  }
}
import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { catchError, debounceTime, finalize, map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AbstractControl, AsyncValidatorFn, ValidationErrors } from '@angular/forms';
import { Router } from '@angular/router';
import { UserLoginModel, UserRegistrationModel } from '../../user-profile/user.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  currentUserToken: any;
  service: any;
  hasAccess: boolean = false;

  private apiUrl = environment.apiUrl;
  private token = environment.tokenKey;
  private username = environment.usernameKey;
  private role = environment.userRoleName;
  private maxInactivityTime = 15 * 60 * 1000; // 15 minutes


  // Authentication state
  private isLoggedInSubject = new BehaviorSubject<boolean>(this.hasValidToken());
  isLoggedIn$ = this.isLoggedInSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {
    this.setupInactivityTimer();

    // window.onbeforeunload = () => {
    //   this.logout();
    // };
  }

  // Token Validation Methods
  hasValidToken(): boolean {
    const token = this.getToken();
    return !!token; // Simply check for token existence (no expiration check)
  }

  // Token and User Management 
  getToken(): string | null {
    return localStorage.getItem(this.token);
  }

  getUsername(): string | null {
    return localStorage.getItem(this.username);
  }

  getUserRole(): string | null {
    return localStorage.getItem(this.role);
  }

  saveUserDetails(token: string, username: string, role: string = ''): void {
    localStorage.setItem(this.token, token);
    localStorage.setItem(this.username, username);
    localStorage.setItem(this.role, role || '');  // Default to '' if role is undefined
    sessionStorage.setItem('lastActivity', Date.now().toString()); // Track last activity
    this.isLoggedInSubject.next(true);
    this.resetInactivityTimer();
  }



  checkServiceAccess(serviceId: number): Observable<boolean> {
    const token = this.getToken();
    if (!token) {
      return of(false); // Return false if no token exists
    }

    const headers = new HttpHeaders().set('Authorization', `${token}`);
    return this.http.get<boolean>(`${environment.apiUrl}/services/check-access/${serviceId}`, { headers });
  }


  // Authentication Methods
  login(credentials: UserLoginModel): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/account/login`, credentials).pipe(
      tap(response => {
        this.saveUserDetails(response.token, response.username, response.role);
      })
    );
  }

  register(userData: UserRegistrationModel): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/account/register`, userData).pipe(
      tap(response => {
        this.saveUserDetails(response.token, response.username, response.role);
      }),
      catchError(this.handleError)
    );
  }


  logout(): void {
    localStorage.removeItem(this.token);
    localStorage.removeItem(this.username);
    localStorage.removeItem(this.role);
    sessionStorage.removeItem('lastActivity');
    this.isLoggedInSubject.next(false);
    this.router.navigate(['/login']);
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
  // Inactivity Timer
  setupInactivityTimer(): void {
    const inactivityCheck = () => {
      const lastActivity = sessionStorage.getItem('lastActivity');
      if (lastActivity && Date.now() - parseInt(lastActivity) > this.maxInactivityTime) {
        this.logout();
      }
    };
    setInterval(inactivityCheck, 60000);
  }

  resetInactivityTimer(): void {
    sessionStorage.setItem('lastActivity', Date.now().toString());
  }

  // Check if the user has the 'ServiceProvider' role
  isServiceProvider(): boolean {
    const role = localStorage.getItem(this.role);
    return role === 'ServiceProvider';
  }

  // Error Handling
  handleError(error: HttpErrorResponse) {
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
        errorMessage = `Error Code: ${error.status}`;
      }
    }

    return throwError(() => new Error(errorMessage));
  }

  // Username Availability Check
  checkUsername(username: string): Observable<{ isUsernameTaken: boolean }> {
    return this.http.get<{ isUsernameTaken: boolean }>(
      `${this.apiUrl}/account/check-username/${username}`
    ).pipe(
      catchError(this.handleError)
    );
  }

  // Username Validator
  usernameValidator(): AsyncValidatorFn {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      if (!control.value) {
        return of(null);  // If no value is provided, no validation is needed.
      }

      return this.checkUsername(control.value).pipe(
        debounceTime(500),  // Debounce for 500ms to wait before making the API call.
        map((response) => {
          return response.isUsernameTaken ? { usernameTaken: true } : null;  // If the username is taken, return error.
        }),
        catchError((error) => {
          console.error('Error during username validation:', error);
          return of({ usernameTaken: false });  // Return false for usernameTaken if error occurs.
        })
      );
    };
  }

  // Email Availability Check
  checkEmail(email: string): Observable<{ isEmailTaken: boolean }> {
    return this.http.get<{ isEmailTaken: boolean }>(
      `${this.apiUrl}/account/check-email/${email}`
    ).pipe(
      catchError(this.handleError)
    );
  }

  // Email Validator
  emailValidator(): AsyncValidatorFn {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      if (!control.value) {
        return of(null);  // If no value is provided, no validation is needed.
      }

      return this.checkEmail(control.value).pipe(
        debounceTime(500),  // Debounce for 500ms to wait before making the API call.
        map((response) => {
          return response.isEmailTaken ? { emailTaken: true } : null;  // If the email is taken, return error.
        }),
        catchError((error) => {
          console.error('Error during email validation:', error);
          return of({ emailTaken: false });  // Return false for emailTaken if error occurs.
        })
      );
    };
  }
}

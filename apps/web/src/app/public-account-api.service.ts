import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, switchMap, tap, throwError, timeout } from 'rxjs';

interface BearerResponse {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

export interface PublicAccountProfile {
  email: string;
}

// Security policy: access token lives in process memory only. No localStorage,
// sessionStorage, cookies, URL parameters or console logs. No refresh token
// persistence; reauthentication is required after reload/session expiry.
@Injectable({ providedIn: 'root' })
export class PublicAccountApi {
  private readonly http = inject(HttpClient);
  private accessToken: string | null = null;
  private expiresAt = 0;
  readonly profile = signal<PublicAccountProfile | null>(null);

  register(email: string, password: string): Observable<void> {
    return this.http.post<void>('/api/public-account/register', { email, password })
      .pipe(timeout(12000));
  }

  resendConfirmation(email: string): Observable<void> {
    return this.http.post<void>('/api/public-account/resend-confirmation', { email })
      .pipe(timeout(12000));
  }

  requestReset(email: string): Observable<void> {
    return this.http.post<void>('/api/public-account/forgot-password', { email })
      .pipe(timeout(12000));
  }

  resetPassword(email: string, resetCode: string, newPassword: string): Observable<void> {
    return this.http.post<void>('/api/public-account/reset-password',
      { email, resetCode, newPassword }).pipe(timeout(12000));
  }

  login(email: string, password: string): Observable<PublicAccountProfile> {
    this.logout();
    return this.http.post<BearerResponse>('/api/public-account/login', { email, password })
      .pipe(
        timeout(12000),
        tap(response => {
          if (!response?.accessToken || response.tokenType?.toLowerCase() !== 'bearer' ||
              !Number.isFinite(response.expiresIn) || response.expiresIn <= 0) {
            throw new Error('AUTH_RESPONSE_INVALID');
          }
          this.accessToken = response.accessToken;
          this.expiresAt = Date.now() + response.expiresIn * 1000;
        }),
        switchMap(() => this.loadProfile())
      );
  }

  loadProfile(): Observable<PublicAccountProfile> {
    if (!this.accessToken || Date.now() >= this.expiresAt - 10000) {
      this.logout();
      return throwError(() => new Error('AUTH_SESSION_EXPIRED'));
    }
    return this.http.get<PublicAccountProfile>('/api/public-account/session', {
      headers: new HttpHeaders({ Authorization: 'Bearer ' + this.accessToken })
    }).pipe(
      timeout(12000),
      tap(profile => {
        if (!profile?.email) throw new Error('AUTH_PROFILE_INVALID');
        this.profile.set(profile);
      })
    );
  }

  logout(): void {
    this.accessToken = null;
    this.expiresAt = 0;
    this.profile.set(null);
  }
}

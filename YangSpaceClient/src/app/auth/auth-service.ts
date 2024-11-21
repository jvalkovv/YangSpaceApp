import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private apiUrl = `${environment.apiUrl}`;

  // Ensure BehaviorSubject is initialized with the correct state
  private isLoggedInSubject = new BehaviorSubject<boolean>(this.hasToken());

  isLoggedIn$ = this.isLoggedInSubject.asObservable(); // Expose as observable
  get isLoggedIn(): boolean {
    return this.isLoggedInSubject.getValue();
  }
  constructor(private http: HttpClient) { }

  // Check if a token exists in localStorage
  hasToken(): boolean {
    return !!localStorage.getItem('token'); // Check if a token exists
  }
  // Register a new user
  register(userData: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, userData);
  }

  // Log in the user
  login(userData: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/login`, userData);
  }
  // Store user information in localStorage during registration/login
  saveUserDetails(token: string, username: string): void {
    localStorage.setItem('token', token);
    localStorage.setItem('username', username);
    this.isLoggedInSubject.next(true);
  }
  // Check if the username is already taken
  checkUsername(username: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/check-username/${username}`);
  }

  // Log out the user
  logout(): void {
    localStorage.clear();
    this.isLoggedInSubject.next(false); // Notify components of logout
  }

  // Fetch user profile
  getUserProfile(): Observable<any> {
    return this.http.get(`${this.apiUrl}/profile`);
  }

  // Update user profile
  updateUserProfile(profileData: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/profile`, profileData);
  }

}


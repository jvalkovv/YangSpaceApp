import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { User } from './user.model';
import { environment } from '../../environments/environment';
import { Service } from '../create-service/service.model';
import { Booking } from '../models/booking.model';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private apiUrl = `${environment.apiUrl}`;

  constructor(private http: HttpClient) { }

  private getAuthHeaders(): HttpHeaders {
    const tokenKey = localStorage.getItem(environment.tokenKey);
    return new HttpHeaders({
      Authorization: `${tokenKey}`,
    });
  }
  // Method to fetch users that are service providers
  getProviders(): Observable<User[]> {
    const headers = this.getAuthHeaders();

    return this.http.get<User[]>(`${this.apiUrl}/services/provider`, { headers });
  }
  
  getUserProfile(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/profile`);
  }

  updateUserProfile(user: User): Observable<User> {
    return this.http.put<User>(`${this.apiUrl}/profile`, user);
  }
  getLastThreeBookings(): Observable<Booking[]> {
    return this.http.get<Booking[]>('/api/bookings/last-three');
  }

  getLastThreeProvidedServices(): Observable<Service[]> {
    return this.http.get<Service[]>('/api/services/last-three');
  }

}

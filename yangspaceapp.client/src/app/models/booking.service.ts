import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError, of } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from '../auth/services/auth-service';
import { Booking } from '../models/booking.model';

@Injectable({
  providedIn: 'root',
})
export class BookingService {
  private apiUrl = `${environment.apiUrl}/booking`; // URL to the booking API

  constructor(private http: HttpClient, private authService: AuthService) { }

  createBooking(booking: Booking): Observable<any> {
    return this.http.post(`${this.apiUrl}`, booking).pipe(
      catchError(error => {
        console.error('Create booking failed:', error);
        return of(null);  // return a fallback value or empty result on error
      })
    );
  }

  // Get bookings based on status, page, and pageSize
  getBookings(status: string = 'all', page: number = 1, pageSize: number = 2): Observable<any> {
    // Construct the URL with query parameters
    const url = `${this.apiUrl}?status=${status}&page=${page}&pageSize=${pageSize}`;
    return this.http.get<any>(url).pipe(
      catchError(error => {
        console.error('Failed to fetch bookings:', error);
        return of({ bookings: [], totalCount: 0 });  // Return an empty result on error
      })
    );
  }
  // Method to get bookings for a provider (assumes providerId is extracted from JWT token)
  getBookingsForProvider(): Observable<Booking[]> {
    const providerId = this.authService.getToken(); // Extract providerId from token or user context
    const headers = new HttpHeaders().set('Authorization', `${providerId}`);

    return this.http.get<Booking[]>(`${this.apiUrl}/provider/${providerId}`, { headers });
  }

  // Method to update the booking status
  updateBookingStatus(booking: Booking): Observable<any> {
    const token = this.authService.getToken();
    const headers = new HttpHeaders().set('Authorization', `${token}`);

    return this.http.patch<any>(`${this.apiUrl}/${booking.id}/status`, booking, { headers });
  }

  deleteBooking(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`).pipe(
      catchError(error => {
        console.error('Delete booking failed:', error);
        return of(null);  // return a fallback value on error
      })
    );
  }
}

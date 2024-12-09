import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError } from 'rxjs';
import { Booking } from '../models/booking.model';
import { environment } from '../../environments/environment';
import { of } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class BookingService {
  checkAccess(serviceId: number) {
    throw new Error('Method not implemented.');
  }
  private apiUrl = `${environment.apiUrl}/booking`; // URL to the booking API

  constructor(private http: HttpClient) { }

  createBooking(booking: Booking): Observable<any> {
    return this.http.post(`${this.apiUrl}`, booking).pipe(
      catchError(error => {
        console.error('Create booking failed:', error);
        return of(null);  // return a fallback value or empty result on error
      })
    );
  }

  getBookings(): Observable<Booking[]> {

    return this.http.get<Booking[]>(`${this.apiUrl}`).pipe(
      catchError(error => {
        console.error('Get bookings failed:', error);
        return of([]);  // return empty array on error
      })
    );
  }

  updateBooking(booking: Booking): Observable<any> {
    return this.http.put(`${this.apiUrl}/${booking.id}`, booking).pipe(
      catchError(error => {
        console.error('Update booking failed:', error);
        return of(null);  // return a fallback value on error
      })
    );
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

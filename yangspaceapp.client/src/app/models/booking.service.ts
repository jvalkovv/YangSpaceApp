import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError } from 'rxjs';
import { Booking, PaginatedBookingsViewModel } from '../models/booking.model';
import { environment } from '../../environments/environment';
import { of } from 'rxjs';
import id from '@angular/common/locales/id';

@Injectable({
  providedIn: 'root',
})
export class BookingService {

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

  getBookings(
    status?: string,
    page: number = 1,
    pageSize: number = 10
  ): Observable<PaginatedBookingsViewModel> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    if (status) {
      params = params.set('status', status);
    }

    return this.http.get<PaginatedBookingsViewModel>(this.apiUrl, { params }).pipe(
      catchError((error) => {
        console.error('Get bookings failed:', error);
        return of({ totalCount: 0, bookings: [] }); // Return an empty paginated object on error
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

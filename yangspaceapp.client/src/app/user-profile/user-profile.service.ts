import { Injectable, Input } from "@angular/core";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { catchError, Observable, of } from "rxjs";
import { environment } from "../../environments/environment";
import { AuthService } from "../auth/services/auth-service";
import { BookingService } from "../models/booking.service";
import { Booking } from "../models/booking.model";
import { Service } from "../create-service/service.model";
import { ServiceService } from "../models/service.service";
import { UserService } from "./user.service";
import { User } from "./user.model";

@Injectable({
  providedIn: 'root',
})
export class UserProfileService {
  private apiUrl = `${environment.apiUrl}/UserProfile/user-profile`;
  private _viewBookings = false;
  private tokenKey = localStorage.getItem(environment.tokenKey) || '';
  lastThreeBookings: Booking[] = [];
  lastThreeProvidedServices: Service[] = [];

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private bookingService: BookingService,
    private serviceService: ServiceService
  ) {}

  private getAuthHeaders(): HttpHeaders {
    return new HttpHeaders({
      Authorization: `${this.tokenKey}`,
    });
  }

  getUserProfile(): Observable<any> {
    const headers = this.getAuthHeaders();
    return this.http.get(this.apiUrl, { headers }).pipe(
      catchError((error) => {
        console.error('Error fetching user profile:', error);
        throw error;
      })
    );
  }

  updateUserProfile(userProfile: any): Observable<any> {
    const headers = this.getAuthHeaders();
    return this.http.put(this.apiUrl, userProfile.username, { headers });
  }

  get viewBookings(): boolean {
    return this._viewBookings;
  }

  toggleBookings(): void {
    this._viewBookings = !this._viewBookings;
  }

  fetchLastThreeUserBookings(): void {
    const currentUserTokenId = this.authService.getToken();

    this.bookingService.getBookings().subscribe({
      next: (bookings) => {
        this.lastThreeBookings = bookings
          .filter((booking) => booking.userToken === currentUserTokenId)
          .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())
          .slice(0, 3);
      },
      error: (err) => {
        console.error('Failed to fetch user bookings:', err);
      },
    });
  }

  fetchLastThreeServicesProvided(): void {
    const currentUserTokenId = this.authService.getToken();
  
    this.bookingService.getBookings().subscribe({
      next: (bookings) => {
        const lastThreeBookings = bookings
          .filter((booking) => booking.providerToken === currentUserTokenId)
          .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())
          .slice(0, 3);
  
        // Fetch the service details for each booking
        lastThreeBookings.forEach((booking) => {
          // Ensure serviceId is valid (not undefined) before calling getServiceById
          const serviceId = booking.serviceId;
          if (serviceId !== undefined) {
            this.serviceService.getServiceById(serviceId).subscribe({
              next: (service) => {
                this.lastThreeProvidedServices.push(service);
              },
              error: (err) => {
                console.error('Failed to fetch service details:', err);
              },
            });
          } else {
            console.error('Service ID is undefined for booking:', booking);
          }
        });
      },
      error: (err) => {
        console.error('Failed to fetch provided services:', err);
      },
    });
  }
  
}

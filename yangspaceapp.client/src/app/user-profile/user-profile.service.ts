import { HttpClient, HttpHeaders } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../environments/environment";
import { AuthService } from "../auth/services/auth-service";
import { Service } from "../create-service/service.model";
import { Booking } from "../models/booking.model";
import { BookingService } from "../models/booking.service";
import { ServiceService } from "../models/service.service";

@Injectable({
  providedIn: 'root',
})
export class UserProfileService {
  private apiUrl = `${environment.apiUrl}/UserProfile`;
  private _viewBookings = false;
  private tokenKey = localStorage.getItem(environment.tokenKey) || '';
  lastThreeBookings: Booking[] = [];
  lastThreeProvidedServices: Service[] = [];

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private bookingService: BookingService,
    private serviceService: ServiceService
  ) { }

  private getAuthHeaders(): HttpHeaders {
    return new HttpHeaders({
      Authorization: `${this.tokenKey}`,
    });
  }

  // Fetch user profile (could be expanded if needed)
  getUserProfile(): Observable<any> {
    const headers = this.getAuthHeaders();
    return this.http.get(`${this.apiUrl}/user-profile`, { headers });
  }

  updateUserProfile(userProfile: any): Observable<any> {
    const headers = this.getAuthHeaders();
    return this.http.put(this.apiUrl, userProfile.username, { headers });
  }

  // Get services that the user is booked for
  getServicesBooked(): Observable<Service[]> {
    const headers = this.getAuthHeaders();
    return this.http.get<Service[]>(`${this.apiUrl}/services-booked`, { headers });
  }

  // Get services that the user is providing (provider services)
  getServicesToProvide(): Observable<Service[]> {
    const headers = this.getAuthHeaders();
    return this.http.get<Service[]>(`${this.apiUrl}/services-to-provide`, { headers });
  }

  toggleBookings(): void {
    this._viewBookings = !this._viewBookings;
  }
}

import { Injectable } from "@angular/core";
import { environment } from "../../environments/environment";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { AuthService } from "../auth/auth-service";
import { Observable } from "rxjs";

@Injectable({
    providedIn: 'root'
  })
  export class UserProfileService {
  
    private apiUrl = `${environment.apiUrl}/profile`;  // Your backend API URL
  
    private _viewBookings: boolean = false;  // Renamed the property to avoid conflict
  
    constructor(
      private http: HttpClient,
      private authService: AuthService
    ) { }
  
    // Fetch user profile
    getUserProfile(): Observable<any> {
      const token = localStorage.getItem('token');
      const headers = new HttpHeaders({
        'Authorization': `Bearer ${token}`
      });
  
      return this.http.get<any>(`${this.apiUrl}/profile`, { headers });
    }
  
    // Update user profile
    updateUserProfile(userProfile: any): Observable<any> {
      const token = localStorage.getItem('token');
      const headers = new HttpHeaders({
        'Authorization': `Bearer ${token}`
      });
  
      return this.http.put<any>(`${this.apiUrl}/profile`, userProfile, { headers });
    }
  
    // Getter and Setter for viewBookings
    get viewBookings(): boolean {
      return this._viewBookings;
    }
  
    toggleBookings(): void {
      this._viewBookings = !this._viewBookings;
    }
  }
  
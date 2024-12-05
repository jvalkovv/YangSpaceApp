import { Injectable } from "@angular/core";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { catchError, Observable } from "rxjs";
import { environment } from "../../environments/environment";

@Injectable({
  providedIn: 'root',
})
export class UserProfileService {
  private apiUrl = `${environment.apiUrl}/UserProfile/user-profile`;
  private _viewBookings = false;
  private tokenKey = localStorage.getItem(environment.tokenKey)  || '';

  constructor(private http: HttpClient) { }

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
}


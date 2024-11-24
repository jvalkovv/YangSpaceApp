import { Injectable } from "@angular/core";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../environments/environment";

@Injectable({
  providedIn: 'root',
})
export class UserProfileService {
  private apiUrl = `${environment.apiUrl}/UserProfile/user-profile`;
  private _viewBookings = false;

  constructor(private http: HttpClient) { }

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      Authorization: `${token}`,
    });
  }

  getUserProfile(): Observable<any> {
    const headers = this.getAuthHeaders();      
    return this.http.get(this.apiUrl, { headers });
  }

  updateUserProfile(userProfile: any): Observable<any> {
    const headers = this.getAuthHeaders();
    return this.http.put(`${this.apiUrl}`, userProfile, { headers });
  }

  get viewBookings(): boolean {
    return this._viewBookings;
  }

  toggleBookings(): void {
    this._viewBookings = !this._viewBookings;
  }
}


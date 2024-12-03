import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { User } from './user.model'; 
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private apiUrl = `${environment.apiUrl}`; 

  constructor(private http: HttpClient) {}
  
  private getAuthHeaders(): HttpHeaders {
    const tokenKey = localStorage.getItem(environment.tokenKey);
    return new HttpHeaders({
      Authorization: `${tokenKey}`,
    });
  }
  // Method to fetch users that are service providers
  getProviders(): Observable<User[]> {
    const headers = this.getAuthHeaders();      

    return this.http.get<User[]>(`${this.apiUrl}/services/provider`, {headers}); 
  }
}

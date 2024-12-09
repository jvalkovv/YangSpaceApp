import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from '../auth/services/auth-service';
import { Service } from '../create-service/service.model';

@Injectable({
  providedIn: 'root',
})
export class ServiceService {
  private apiUrl = `${environment.apiUrl}/services`; // URL to the services API

  constructor(private http: HttpClient, private authService: AuthService) { }

  // Fetch all services
  getServices(): Observable<Service[]> {
    return this.http.get<Service[]>(`${this.apiUrl}`);
  }

  // Create a new service using FormData
  createService(formData: FormData): Observable<any> {
    const token = this.authService.getToken(); // Retrieve token from AuthService
    const headers = { Authorization: `${token}` }; // Make sure to prefix with 'Bearer'

    return this.http.post(`${this.apiUrl}/create-service`, formData, { headers });
  }

  // Update a service by ID
  updateService(serviceId: number, formData: FormData): Observable<any> {
    const token = this.authService.getToken(); // Retrieve token from AuthService
    const headers = { Authorization: `${token}` }; 
  
    return this.http.put(`${this.apiUrl}/edit-service/:${serviceId}`, formData, { headers });
  }

  // Delete a service by ID
  deleteService(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/delete-service/${id}`);
  }

  getServiceById(serviceId: number): Observable<Service> {
    return this.http.get<Service>(`${this.apiUrl}/${serviceId}`);
  }
}

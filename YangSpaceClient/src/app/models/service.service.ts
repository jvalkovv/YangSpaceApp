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
  private apiUrl = `${environment.apiUrl}/services/create-service`; // URL to the services API

  constructor(private http: HttpClient, private authService: AuthService) {}

  getServices(): Observable<Service[]> {
    return this.http.get<Service[]>(`${this.apiUrl}`); // Fetch all services
  }

  createService(service: Service): Observable<any> {
  const token = this.authService.getToken(); // Retrieve token from AuthService
  const headers = { Authorization: `${token}` };

  return this.http.post(`${this.apiUrl}`, service, { headers });
  }

  updateService(service: Service): Observable<any> {
    return this.http.put(`${this.apiUrl}/${service.id}`, service); // Update service by ID
  }

  deleteService(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`); // Delete service by ID
  }
}


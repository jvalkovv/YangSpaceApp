import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Service } from '../models/service.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class ServiceService {
  private apiUrl = `${environment.apiUrl}/services`; // URL to the services API

  constructor(private http: HttpClient) {}

  getServices(): Observable<Service[]> {
    return this.http.get<Service[]>(`${this.apiUrl}`); // Fetch all services
  }

  createService(service: Service): Observable<any> {
    return this.http.post(`${this.apiUrl}`, service); // Add a new service
  }

  updateService(service: Service): Observable<any> {
    return this.http.put(`${this.apiUrl}/${service.id}`, service); // Update service by ID
  }

  deleteService(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`); // Delete service by ID
  }
}

import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { CommonModule } from '@angular/common';
import { ServiceItemComponent } from '../service-item/service-item.component';
import { NavbarComponent } from '../shared/components/navbar/navbar.component';
import { FooterComponent } from '../shared/components/footer/footer.component';
@Component({
  selector: 'app-all-services',
  standalone: true,
  imports: [CommonModule, NavbarComponent, FooterComponent, ServiceItemComponent],
  templateUrl: './all-services.component.html',
  styleUrls: ['./all-services.component.css'],
})
export class AllServicesComponent implements OnInit {
  services: any[] = [];
  totalCount: number = 0;
  errorMessage: string = '';
  currentPage: number = 1;
  pageSize: number = 8;
  loading: boolean = false;

  constructor(private http: HttpClient) { }

  ngOnInit(): void {
    this.loadServices();
  }
  trackById(index: number, service: any): number {
    return service.id; 
  }
  loadServices(page: number = 1): void {
    this.loading = true;

    const url = `${environment.apiUrl}/services/all-services?page=${page}&pageSize=${this.pageSize}`;
    this.http.get<any>(url).subscribe({
      next: (response) => {
        this.services = response.services.map((service: any) => ({
          ...service,
          categoryName: service.categoryName || 'Unknown Category',
          providerName: service.providerName || 'Unknown Provider',
        }));
        this.totalCount = response.totalCount;
        this.loading = false;
      },
      error: () => {
        if (this.totalCount <= 0) {
          this.errorMessage = 'No services available at the moment.'
        }
        else {
          this.errorMessage = 'Failed to load services.';
        }
        this.loading = false;
      },
    });
  }

  nextPage(): void {
    if (this.currentPage * this.pageSize < this.totalCount) {
      this.currentPage++;
      this.loadServices(this.currentPage);
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadServices(this.currentPage);
    }
  }
}

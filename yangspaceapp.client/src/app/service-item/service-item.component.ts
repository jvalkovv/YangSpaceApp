import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BookingService } from '../models/booking.service';
import { AuthService } from '../auth/services/auth-service';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { DialogComponent } from '../dialog/dialog.component';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ServiceService } from '../models/service.service';

@Component({
  selector: 'app-service-item',
  standalone: true,
  imports: [CommonModule, FormsModule, DialogComponent],
  templateUrl: './service-item.component.html',
  styleUrls: ['./service-item.component.css'],
})
export class ServiceItemComponent {
  @Input() service: any; 
  isBooking = false;
  isLoading = false;
  selectedTime = '12:00 PM';
  bookingDate: string = '';
  dialogMessage: string = '';
  dialogTitle: string = '';
  isDialogVisible: boolean = false;
  hasAccess: boolean = false;
  currentUserToken!: string | any;

  constructor(
    private bookingService: BookingService,
    private authService: AuthService,
    private router: Router,
    private http: HttpClient,
    private serviceService: ServiceService
  ) { }

  ngOnInit(): void {
    this.authService.checkServiceAccess(this.service.serviceId).subscribe(
      response => {
        this.hasAccess = response;
        console.log('Access check result:', this.hasAccess);
      },
      error => {
        console.error('Error:', error);
        this.hasAccess = false;
      }
    );


  }
  
  bookService(serviceId: number): void {
    this.isBooking = true;

    this.serviceService.bookService(serviceId).subscribe({
      next: (response: { message: any; }) => {
        alert(response.message);
        this.isBooking = false;
      },
      error: (error) => {
        alert(error.error?.error || 'Failed to book the service.');
        this.isBooking = false;
      },
    });
  }
  checkUserAccess(): any {
    if (!this.currentUserToken || !this.service?.serviceId) {
      this.hasAccess = false;
      return;
    }

    const headers = new HttpHeaders().set('Authorization', `${this.currentUserToken}`);

    // Send request to check access for the specific service
    this.http.get<boolean>(`${environment.apiUrl}/services/check-access/${this.service.serviceId}`, { headers })
      .subscribe(
        (response: boolean) => {
          this.hasAccess = response
          console.log(response);

        },
        error => {
          console.error('Error:', error);
          this.hasAccess = false;
        }
      );
  }

  canEditService(): boolean {
    return this.service?.userToken === this.authService.getToken();
  }
  onEditService(serviceId: number): void {
    // Navigate to the edit service page (or perform the required action)
    this.router.navigate(['/edit-service', serviceId]);
  }

  openDialog(message: string, title: string): void {
    this.dialogMessage = message;
    this.dialogTitle = title;
    this.isDialogVisible = true;
  }

  closeDialog(): void {
    this.isDialogVisible = false;
  }

  get imageUrl(): string {
    return this.service?.imageUrl
      ? `${environment.imageUrl}/${this.service.imageUrl}`
      : '';
  }

}

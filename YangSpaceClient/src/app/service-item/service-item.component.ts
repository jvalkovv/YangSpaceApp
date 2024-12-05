import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Booking } from '../models/booking.model';
import { BookingService } from '../models/booking.service';
import { AuthService } from '../auth/services/auth-service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FormsModule } from '@angular/forms'; // Import FormsModule
import { Router } from '@angular/router';
import { Service } from '../create-service/service.model';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-service-item',
  standalone: true,
  imports: [CommonModule, FormsModule], // Ensure FormsModule is imported
  templateUrl: './service-item.component.html',
  styleUrls: ['./service-item.component.css'],
})
export class ServiceItemComponent {
  @Input() service!: Service;
  currentUser: any;
  isLoading = false;
  selectedTime = '12:00 PM';
  bookingDate: string = '';

  constructor(
    private bookingService: BookingService,
    private authService: AuthService,
    private snackBar: MatSnackBar,
    private router: Router
  ) {
    this.currentUser = this.authService.getToken();

  }

  get imageUrl(): string {
    if (this.service && this.service.imageUrl) {
      return `${environment.imageUrl}/${this.service.imageUrl}`;
    }
    return ''; 
  }

  onEditService(serviceId: number | undefined): void {
    if (serviceId === undefined) {
      this.snackBar.open('Invalid service id.', 'Error');
      return;
    }
    this.router.navigate(['/edit-service', serviceId]);
  }

  bookService(): void {
    const currentUserTokenId = this.authService.getToken();
    console.log(this.imageUrl);
    if (!currentUserTokenId) {
      this.snackBar.open('You need to log in to book a service.', 'Login Required');
      return;
    }

    if (!this.bookingDate) {
      this.snackBar.open('Please select a booking date.', 'Date Required');
      return;
    }

    this.isLoading = true;

    const booking: Booking = {
      serviceId: this.service.id,
      userToken: currentUserTokenId,
      status: 'pending',
      date: this.bookingDate,
      time: this.selectedTime
    };

    this.bookingService.createBooking(booking).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response) {
          this.snackBar.open('Booking created successfully!', 'Success');
        } else {
          this.snackBar.open('Failed to create booking. Try again.', 'Error');
        }
      },
      error: () => {
        this.isLoading = false;
        this.snackBar.open('An error occurred while creating the booking.', 'Error');
      }
    });
  }
}

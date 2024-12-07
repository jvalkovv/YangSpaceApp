import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Booking } from '../models/booking.model';
import { BookingService } from '../models/booking.service';
import { AuthService } from '../auth/services/auth-service';
import { FormsModule } from '@angular/forms'; 
import { Router } from '@angular/router';
import { Service } from '../create-service/service.model';
import { environment } from '../../environments/environment';
import { DialogComponent } from "../dialog/dialog.component";

@Component({
  selector: 'app-service-item',
  standalone: true,
  imports: [CommonModule, FormsModule, DialogComponent],
  templateUrl: './service-item.component.html',
  styleUrls: ['./service-item.component.css'],
})
export class ServiceItemComponent {
  @Input() service!: Service;
  currentUser: any;
  isLoading = false;
  selectedTime = '12:00 PM';
  bookingDate: string = '';
  dialogMessage: string = '';
  dialogTitle: string = '';
  isDialogVisible: boolean = false;

  constructor(
    private bookingService: BookingService,
    private authService: AuthService,
    private router: Router,
  ) {
    this.currentUser = this.authService.getToken();
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
    if (this.service && this.service.imageUrl) {
      return `${environment.imageUrl}/${this.service.imageUrl}`;
    }
    return ''; 
  }
  canEditService(): boolean {
    return this.service.userToken === this.currentUser.userToken;
  }

  onEditService(serviceId: number | undefined): void {
    if (!serviceId) {
      this.openDialog(`${serviceId}'Service ID is invalid.'`, 'Error');
      return;
    }
    this.router.navigate(['/edit-service', serviceId]); // Route to the edit page
  }

  bookService(): void {
    const currentUserTokenId = this.authService.getToken();
    if (!currentUserTokenId) {
      this.openDialog('You need to log in to book a service.', 'Login Required');
      return;
    }

    if (!this.bookingDate) {
      this.openDialog('Please select a booking date.', 'Date Required');
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
          this.openDialog('Booking created successfully!', 'Success');
        } else {
          this.openDialog('Failed to create booking. Try again.', 'Error');
        }
      },
      error: () => {
        this.isLoading = false;
        this.openDialog('An error occurred while creating the booking.', 'Error');
      }
    });
  }
}

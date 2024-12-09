import { Component, OnInit } from '@angular/core';
import { Booking } from '../models/booking.model';
import { BookingService } from '../models/booking.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { FilterByStatusPipe } from './filterByStatus';
import { AuthService } from '../auth/services/auth-service';

@Component({
  selector: 'app-booked-services',
  standalone: true,
  imports: [FormsModule, CommonModule, FilterByStatusPipe],
  templateUrl: './booked-services.component.html',
  styleUrls: ['./booked-services.component.css']
})
export class BookedServicesComponent implements OnInit {
  bookings: Booking[] = [];  // To store the list of bookings
  statusFilter: any;
  id: number | undefined;
  currentUserTokenId: string | null|undefined;

  constructor(private bookingService: BookingService, private authService: AuthService) { }

 
  ngOnInit(): void {
    this.currentUserTokenId = this.authService.getToken();
    this.fetchBookings();
  }
  fetchBookings(): void {
    if (!this.currentUserTokenId) {
      alert('You need to log in to view your bookings.');
      return;
    }
  
    this.bookingService.getBookings().subscribe({
      next: (bookings) => {
        this.bookings = bookings.filter(
          (booking) => booking.userToken === this.currentUserTokenId
        );
      },
      error: (err) => {
        console.error('Failed to fetch bookings:', err);
      },
    });
  }

  deleteBooking(id: number | undefined): void {
    if (!id) {
      alert('Invalid booking ID. Cannot cancel.');
      return;
    }
  
    this.bookingService.deleteBooking(id).subscribe(
      () => {
        // Remove the deleted booking from the list
        this.bookings = this.bookings.filter((booking) => booking.id !== id);
      },
      (error) => {
        console.error('Failed to delete booking:', error);
        alert('Failed to cancel booking. Please try again later.');
      }
    );
  }

  // toggleBookings(): void {
  //   this.viewBookings = !this.viewBookings;
  //   if (this.viewBookings) {
  //     this.fetchBookedTasks();
  //   } else {
  //     this.bookedTasks = [];
  //   }
  // }

 
}

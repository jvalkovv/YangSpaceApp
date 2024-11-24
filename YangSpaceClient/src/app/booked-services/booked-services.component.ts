import { Component, OnInit } from '@angular/core';
import { Booking } from '../models/booking.model';
import { BookingService } from '../models/booking.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { FilterByStatusPipe } from './filterByStatus';

@Component({
  selector: 'app-booked-services',
  standalone: true,
  imports: [FormsModule, CommonModule, FilterByStatusPipe],
  templateUrl: './booked-services.component.html',
  styleUrls: ['./booked-services.component.css']
})
export class BookedServicesComponent implements OnInit {
  bookings: Booking[] = [];  // To store the list of bookings
  currentUserId: number = 1; // Replace this with actual user ID from your auth service
   statusFilter: any;
  
  constructor(private bookingService: BookingService) {}

  ngOnInit(): void {
    // Fetch all bookings for the user when the component loads
    this.bookingService.getBookings().subscribe(bookings => {
      // Filter bookings by current user (if needed)
      this.bookings = bookings.filter(booking => booking.userId === this.currentUserId);
    });
  }
  deleteBooking(id: number): void {
    this.bookingService.deleteBooking(id).subscribe(() => {
      this.bookings = this.bookings.filter(booking => booking.id !== id);  // Remove deleted booking from the list
    });
  }
   // toggleBookings(): void {
  //   this.viewBookings = !this.viewBookings;
  //   if (this.viewBookings) {
  //     this.fetchBookedTasks();
  //   } else {
  //     this.bookedTasks = [];
  //   }
  // }

  // fetchBookedTasks(): void {
  //   this.userProfileService.toggleBookings()
  //     .subscribe({
  //       next: (data) => (this.bookedTasks = data),
  //       error: (err) => {
  //         console.error('Error fetching booked tasks:', err);
  //         alert('Failed to fetch booked tasks');
  //       },
  //     });
  // }
  
  // Add additional methods for filtering, sorting, etc. if needed
}

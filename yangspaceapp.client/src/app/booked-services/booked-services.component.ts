import { Component, OnInit } from '@angular/core';
import { Booking } from '../models/booking.model';
import { BookingService } from '../models/booking.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { FilterByStatusPipe } from './filterByStatus';
import { AuthService } from '../auth/services/auth-service';
import { FooterComponent } from '../shared/components/footer/footer.component';
import { NavbarComponent } from '../shared/components/navbar/navbar.component';

@Component({
  selector: 'app-booked-services',
  standalone: true,
  imports: [FormsModule, CommonModule, FilterByStatusPipe, FooterComponent, NavbarComponent],
  templateUrl: './booked-services.component.html',
  styleUrls: ['./booked-services.component.css']
})
export class BookedServicesComponent implements OnInit {
  bookings: Booking[] = [];  // To store the list of bookings
  statusFilter: any;
  isLoading: boolean = false; // Add this line
  currentUserTokenId: string | null | undefined;

  constructor(private bookingService: BookingService, private authService: AuthService) { }


  ngOnInit(): void {
    this.currentUserTokenId = this.authService.getToken();
    this.fetchBookings();

  }
  fetchBookings(status?: string): void {
    const page = 1; // Set the page number
    const pageSize = 10; // Set the page size

    this.isLoading = true; // Set loading to true when data is being fetched

    this.bookingService.getBookings(status, page, pageSize).subscribe({
      next: (response) => {
        this.bookings = response.bookings; // Populate the bookings list
        this.isLoading = false; // Set loading to false when data is loaded
      },
      error: (err) => {
        console.error('Failed to fetch bookings:', err);
        this.isLoading = false; // Set loading to false in case of error
      }
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


}

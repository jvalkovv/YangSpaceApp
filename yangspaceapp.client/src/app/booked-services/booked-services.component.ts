import { Component, OnInit } from '@angular/core';
import { Booking, BookingStatus } from '../models/booking.model';
import { BookingService } from '../models/booking.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { FilterByStatusPipe } from './filterByStatus';
import { AuthService } from '../auth/services/auth-service';
import { FooterComponent } from '../shared/components/footer/footer.component';
import { NavbarComponent } from '../shared/components/navbar/navbar.component';
import { CeilPipe } from './ceil.pipe';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-booked-services',
  standalone: true,
  imports: [FormsModule, CommonModule, FilterByStatusPipe, FooterComponent, NavbarComponent, CeilPipe],
  templateUrl: './booked-services.component.html',
  styleUrls: ['./booked-services.component.css']
})
export class BookedServicesComponent implements OnInit {
  bookings: Booking[] = [];  // To store the list of bookings
  statusFilter: BookingStatus | string = 'all';
  currentUserTokenId: string | null | undefined;
  isLoading: boolean = false; // Add this line
  pageSize: number = 5;
  currentPage: number = 1;
  totalCount: number = 0;
  statusEnum = Object.values(BookingStatus);
  constructor(private bookingService: BookingService, private authService: AuthService) { }


  ngOnInit(): void {
    this.currentUserTokenId = this.authService.getToken();
    this.fetchBookings();

  }

  fetchBookings(status: string | null = null): void {
    this.isLoading = true;

    // Call getBookings method from service
    this.bookingService.getBookings(this.statusFilter as string, this.currentPage, this.pageSize).subscribe({
      next: (response) => {
        this.bookings = response.bookings;
        this.totalCount = response.totalCount;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to fetch bookings:', err);
        this.isLoading = false;
      }
    });
  }


  goToPage(page: number): void {
    if (page >= 1 && page <= Math.ceil(this.totalCount / this.pageSize)) {
      this.currentPage = page;
      this.fetchBookings(this.statusFilter);  // Ensure statusFilter is passed
    }
  }
  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.fetchBookings(this.statusFilter); // Fetch bookings for the previous page with the selected status
    }
  }

  nextPage(): void {
    if (this.currentPage * this.pageSize < this.totalCount) {
      this.currentPage++;
      this.fetchBookings(this.statusFilter);  // Ensure statusFilter is passed
    }
  }

  onStatusChange(): void {
    this.currentPage = 1;  // Reset to the first page when the filter changes
    this.fetchBookings();  // Fetch bookings based on the selected status
  }



}

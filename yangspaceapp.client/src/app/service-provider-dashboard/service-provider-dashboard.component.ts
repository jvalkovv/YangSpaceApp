import { Component, OnInit } from '@angular/core';
import { Booking } from '../models/booking.model';
import { BookingService } from '../models/booking.service';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../shared/components/navbar/navbar.component';
import { FooterComponent } from '../shared/components/footer/footer.component';

@Component({
  selector: 'app-service-provider-dashboard',
  standalone: true,
  imports: [CommonModule, NavbarComponent, FooterComponent],
  templateUrl: './service-provider-dashboard.component.html',
  styleUrls: ['./service-provider-dashboard.component.css']
})
export class ServiceProviderDashboardComponent implements OnInit {
  bookings: Booking[] = [];


  constructor(private bookingService: BookingService) { }

  ngOnInit(): void {
    this.loadBookings();
  }

  loadBookings(): void {
    this.bookingService.getBookingsForProvider().subscribe((bookings) => {
      this.bookings = bookings;
    });
  }

  startService(booking: Booking): void {
    booking.status = 'inprogress';
    this.bookingService.updateBookingStatus(booking).subscribe(() => {
      this.loadBookings();
 
    });
  }

  completeService(booking: Booking): void {
    booking.status = 'completed';
    booking.resolvedDate = new Date();  // Set resolved date when completed
    this.bookingService.updateBookingStatus(booking).subscribe(() => {
      this.loadBookings();  // Reload bookings to get updated status
    });
  }
}

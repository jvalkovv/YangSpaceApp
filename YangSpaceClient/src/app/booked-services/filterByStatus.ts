import { Pipe, PipeTransform } from '@angular/core';
import { Booking } from '../models/booking.model';

@Pipe({
  name: 'filterByStatus',
  standalone: true, // Marking the pipe as standalone
})
export class FilterByStatusPipe implements PipeTransform {

  transform(bookings: Booking[], status: string): Booking[] {
    if (status === 'all') {
      return bookings;
    }
    return bookings.filter(booking => booking.status === status);
  }
}

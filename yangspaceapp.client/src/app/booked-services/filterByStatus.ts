import { Pipe, PipeTransform } from '@angular/core';
import { Booking } from '../models/booking.model';

@Pipe({
  name: 'filterByStatus',
  standalone: true,
})
export class FilterByStatusPipe implements PipeTransform {
  transform(bookings: Booking[], status: string): Booking[] {
    if (!status || status === 'all') {
      return bookings;
    }
    return bookings.filter((booking) => booking.status === status);
  }
}

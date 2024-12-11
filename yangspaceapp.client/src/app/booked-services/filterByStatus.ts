import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'filterByStatus',
  standalone: true,
})
export class FilterByStatusPipe implements PipeTransform {
  transform(bookings: any[], status: string | null): any[] {
    if (!status || status === 'all') {
      return bookings;
    }
    return bookings.filter(booking => booking.status === status);
  }
}

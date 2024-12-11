export interface Booking {
  id?: number;
  status: string;
  serviceName: string;
  bookingDate: string;
  serviceId: number | undefined;
  userToken: string;
  date: string;
  time: string;
  providerToken?: string;
  price: number;
}
export interface PaginatedBookingsViewModel {
  totalCount: number;
  bookings: Booking[];
}

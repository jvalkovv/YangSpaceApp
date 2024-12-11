export interface Booking {
  resolvedDate: Date;
  clientEmail: string;
  clientName: string;
  providerEmail: string;
  id?: number;
  status: string;
  serviceName: string;
  bookingDate: string;
  serviceId: number | undefined;
  userToken: string;
  updatedDate: string;
  providerToken?: string;
  price: number;

}
export interface PaginatedBookingsViewModel {
  totalCount: number;
  bookings: Booking[];
}

export enum BookingStatus {
  Pending = 'Pending',
  InProgress = 'InProgress',
  Completed = 'Completed'
}

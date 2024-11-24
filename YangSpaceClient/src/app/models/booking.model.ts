export interface Booking {
    id?: number; // Optional since it's auto-generated by the backend
    status: string;
    serviceId: number; // The service being booked
    userId: number; // The user making the booking
    date: string; // Date of the booking
    time: string; // Time of the booking
  }
  
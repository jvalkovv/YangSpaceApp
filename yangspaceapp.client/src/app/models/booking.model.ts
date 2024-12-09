export interface Booking {
  id?: number;
  status: string;
  serviceId: number | undefined ;
  userToken: string;
  date: string;
  time: string;
  providerToken?: string; // Add this if it's required in the logic
}

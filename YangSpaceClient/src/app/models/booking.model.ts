export interface Booking {
  id?: number;
  status: string;
  serviceId: number | undefined ;
  userToken: string;
  date: string;
  time: string;
}

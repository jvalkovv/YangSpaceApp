export interface Service {
    id?: number; // Optional since it's auto-generated by the backend
    name: string; // Name of the service
    description: string; // Description of the service
    price: number; // Price of the service
    providerId: number; // ID of the service provider
  }
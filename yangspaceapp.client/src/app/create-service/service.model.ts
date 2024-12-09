export interface Service {
  services: Service[]; // List of services
  totalCount: number; // Total count of available services
  id?: number; // Optional ID, for services that may be auto-generated
  serviceId: number;
  title: string; // Title of the service
  description: string; // Description of the service
  price: number; // Price of the service
  categoryId: number; // Category identifier from the backend
  providerId?: string; // Provider identifier, optional
  providerName: string; // Name of the provider offering the service
  categoryName: string; // Name of the category
  imageUrl: string; // URL to uploaded image (e.g., for service images)
  userToken: string;
  email?: string; // Add email property
  phoneNumber?: string;
}
export interface Service {
  services: any[];
  totalCount: number;
  id?: number; // Optional because it might be auto-generated
  title: string;
  description: string;
  price: number;
  categoryId: number; // Backend category identifier
  providerId?: string; // Backend user/provider identifier
  providerName: string;
  categoryName: string;
}
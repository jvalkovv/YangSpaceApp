import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export interface LocationCard {
  id: string;
  slug: string;
  name: string;
  neighborhood: string;
  category: string;
  description: string;
  address: string;
  imageUrl: string | null;
  isVerified: boolean;
  activeOffers: number;
  activeJobs: number;
}
export interface LocalOffer {
  id: string; title: string; description: string; startsAtUtc: string; endsAtUtc: string;
}
export interface LocalBrochure {
  id: string; title: string; url: string; validFromUtc: string; validToUtc: string;
}
export interface LocalJob {
  id: string; title: string; description: string; applyUrl: string; expiresAtUtc: string;
}
export interface LocationDetail extends Omit<LocationCard, 'activeOffers' | 'activeJobs'> {
  city: string; phone: string | null; website: string | null;
  openingHours: string | null; updatedAtUtc: string;
  offers: LocalOffer[]; brochures: LocalBrochure[]; jobs: LocalJob[];
}
export interface DirectoryMeta {
  cities: string[]; neighborhoods: string[]; categories: string[];
}
export interface Paged<T> {
  items: T[]; page: number; pageSize: number; total: number;
}

@Injectable({ providedIn: 'root' })
export class DirectoryApi {
  private readonly http = inject(HttpClient);
  meta() { return this.http.get<DirectoryMeta>('/api/v1/meta'); }
  search(filters: { neighborhood?: string; category?: string; q?: string; page?: number }) {
    let params = new HttpParams().set('page', filters.page ?? 1).set('pageSize', 12);
    if (filters.neighborhood) params = params.set('neighborhood', filters.neighborhood);
    if (filters.category) params = params.set('category', filters.category);
    if (filters.q) params = params.set('q', filters.q);
    return this.http.get<Paged<LocationCard>>('/api/v1/locations', { params });
  }
  location(slug: string) { return this.http.get<LocationDetail>(`/api/v1/locations/${encodeURIComponent(slug)}`); }
}

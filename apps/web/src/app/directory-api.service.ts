import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { TimeoutError, timeout } from 'rxjs';

/** Public-facing copy only. Operational diagnostics belong in server-side logs. */
export function describeDirectoryFailure(reason: unknown): string {
  if (reason instanceof TimeoutError) {
    return 'Зареждането се забави. Опитай отново след малко.';
  }
  if (reason instanceof HttpErrorResponse) {
    if (reason.status === 0) return 'Няма връзка със сървъра. Провери интернет връзката си и опитай отново.';
    if ([502, 503, 504].includes(reason.status)) return 'Услугата е временно недостъпна. Опитай отново след малко.';
    if (reason.status === 404) return 'Търсената информация не е налична. Опитай отново.';
    if (reason.status >= 500) return 'Възникна временен проблем. Опитай отново след малко.';
    return 'Неуспешна заявка. Опитай отново.';
  }
  return 'Не успяхме да заредим информацията. Опитай отново.';
}

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
export interface PublicHighlight {
  slug: string;
  locationName: string;
  neighborhood: string;
  category: string;
  title: string;
  expiresAtUtc: string;
}
export interface HomepagePayload {
  meta: DirectoryMeta;
  directory: Paged<LocationCard>;
  offers: PublicHighlight[];
  brochures: PublicHighlight[];
  jobs: PublicHighlight[];
}

const REQUEST_TIMEOUT_MS = 10000;

@Injectable({ providedIn: 'root' })
export class DirectoryApi {
  private readonly http = inject(HttpClient);

  homepage() {
    return this.http.get<HomepagePayload>('/api/v1/homepage').pipe(timeout(REQUEST_TIMEOUT_MS));
  }
  meta() {
    return this.http.get<DirectoryMeta>('/api/v1/meta').pipe(timeout(REQUEST_TIMEOUT_MS));
  }
  search(filters: { neighborhood?: string; category?: string; q?: string; page?: number }) {
    let params = new HttpParams().set('page', filters.page ?? 1).set('pageSize', 12);
    if (filters.neighborhood) params = params.set('neighborhood', filters.neighborhood);
    if (filters.category) params = params.set('category', filters.category);
    if (filters.q) params = params.set('q', filters.q);
    return this.http.get<Paged<LocationCard>>('/api/v1/locations', { params }).pipe(timeout(REQUEST_TIMEOUT_MS));
  }
  location(slug: string) {
    return this.http.get<LocationDetail>(`/api/v1/locations/${encodeURIComponent(slug)}`).pipe(timeout(REQUEST_TIMEOUT_MS));
  }
}

import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { TimeoutError, timeout } from 'rxjs';

/** UI-facing diagnostics only; never expose stack traces or raw proxy bodies. */
export function describeDirectoryFailure(reason: unknown): string {
  if (reason instanceof TimeoutError) {
    return 'Заявката отне твърде дълго. Провери дали локалното .NET API отговаря и опитай отново.';
  }
  if (reason instanceof HttpErrorResponse) {
    if (reason.status === 0) return 'Няма връзка с API. Провери .NET процеса на 127.0.0.1:5090 и Angular proxy.';
    if ([502, 503, 504].includes(reason.status)) return 'API или локалният proxy е временно недостъпен (HTTP ' + reason.status + '). Стартирай API и опитай отново.';
    if (reason.status === 404) return 'Тази API версия не поддържа заявката (HTTP 404). Провери дали frontend и backend са от един commit.';
    if (reason.status >= 500) return 'API върна вътрешна грешка (HTTP ' + reason.status + '). Провери .NET терминала.';
    return 'Заявката беше отказана (HTTP ' + reason.status + ').';
  }
  return 'Неуспешно зареждане. Провери връзката с локалното API и опитай отново.';
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

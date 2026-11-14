import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import {
  ChangeDetectorRef, Component, DestroyRef, Input, OnChanges, SimpleChanges, inject
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { timeout } from 'rxjs';

type Kind = 'offers' | 'brochures' | 'jobs';
interface BasePublication { id: string; title: string; isPublished: boolean }
interface Offer extends BasePublication { description: string; startsAtUtc: string; endsAtUtc: string }
interface Brochure extends BasePublication { url: string; validFromUtc: string; validToUtc: string }
interface Job extends BasePublication { description: string; applyUrl: string; expiresAtUtc: string }
interface Content { offers: Offer[]; brochures: Brochure[]; jobs: Job[] }
const emptyContent = (): Content => ({ offers: [], brochures: [], jobs: [] });

@Component({
  selector: 'ys-admin-publications',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-publications.component.html'
})
export class AdminPublicationsComponent implements OnChanges {
  @Input() locationId = '';
  @Input() token = '';
  @Input() revision = 0;

  private readonly http = inject(HttpClient);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private requestVersion = 0;
  private readonly requestTimeout = 10000;

  content: Content = emptyContent();
  loading = false;
  busy = false;
  message = '';
  isError = false;
  editKind: Kind | null = null;
  editId = '';
  editTitle = '';
  editDescription = '';
  editUrl = '';
  editFrom = '';
  editTo = '';
  editPublished = false;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['locationId'] || changes['token']) {
      this.content = emptyContent();
      this.editKind = null;
      this.editId = '';
      this.message = '';
    }
    if (changes['locationId'] || changes['token'] || changes['revision']) this.reload();
  }

  private get auth() {
    return { headers: new HttpHeaders({ Authorization: 'Bearer ' + this.token }) };
  }

  private endpoint(kind?: Kind, itemId?: string, action?: string): string {
    let url = '/api/v1/admin/locations/' + encodeURIComponent(this.locationId);
    url += kind ? '/' + kind : '/content';
    if (itemId) url += '/' + encodeURIComponent(itemId);
    if (action) url += '/' + action;
    return url;
  }

  private feedback(message: string, error = false): void {
    this.message = message;
    this.isError = error;
    this.cdr.markForCheck();
  }

  private failure(reason: unknown): void {
    const status = reason instanceof HttpErrorResponse ? reason.status : 0;
    this.feedback(status === 401 || status === 403
      ? 'Сесията е невалидна или няма достъп до публикациите.'
      : status === 409
        ? 'Публикацията е изтекла. Коригирай срока преди възстановяване.'
        : status === 400
          ? 'Невалидни полета или период.'
          : 'Заявката не успя (HTTP ' + status + ').', true);
  }

  reload(): void {
    const version = ++this.requestVersion;
    if (!this.locationId || !this.token) {
      this.loading = false;
      this.content = emptyContent();
      this.cdr.markForCheck();
      return;
    }
    const scopeId = this.locationId;
    const scopeToken = this.token;
    this.loading = true;
    this.cdr.markForCheck();
    this.http.get<Content>(this.endpoint(), this.auth)
      .pipe(timeout(this.requestTimeout), takeUntilDestroyed(this.destroyRef)).subscribe({
        next: value => {
          if (version !== this.requestVersion || scopeId !== this.locationId || scopeToken !== this.token) return;
          this.content = value;
          this.loading = false;
          this.cdr.markForCheck();
        },
        error: reason => {
          if (version !== this.requestVersion || scopeId !== this.locationId) return;
          this.loading = false;
          this.content = emptyContent();
          this.failure(reason);
        }
      });
  }

  kindLabel(kind: Kind): string {
    return kind === 'offers' ? 'оферта' : kind === 'brochures' ? 'брошура' : 'обява за работа';
  }
  trackItem(_index: number, item: BasePublication): string { return item.id; }

  expired(kind: Kind, item: Offer | Brochure | Job): boolean {
    const expiry = kind === 'offers' ? (item as Offer).endsAtUtc
      : kind === 'brochures' ? (item as Brochure).validToUtc : (item as Job).expiresAtUtc;
    return new Date(expiry).getTime() <= Date.now();
  }
  state(kind: Kind, item: Offer | Brochure | Job): string {
    if (!item.isPublished) return 'Архивирана';
    if (this.expired(kind, item)) return 'Изтекла';
    if (kind === 'offers' && new Date((item as Offer).startsAtUtc).getTime() > Date.now()) return 'Планирана';
    if (kind === 'brochures' && new Date((item as Brochure).validFromUtc).getTime() > Date.now()) return 'Планирана';
    return 'Активна';
  }

  private localDateTime(utc: string): string {
    const value = new Date(utc);
    if (!Number.isFinite(value.getTime())) return '';
    return new Date(value.getTime() - value.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
  }
  begin(kind: Kind, item: Offer | Brochure | Job): void {
    if (this.busy) return;
    this.editKind = kind;
    this.editId = item.id;
    this.editTitle = item.title;
    this.editPublished = item.isPublished;
    this.editDescription = kind === 'brochures' ? '' : (item as Offer | Job).description;
    this.editUrl = kind === 'brochures' ? (item as Brochure).url
      : kind === 'jobs' ? (item as Job).applyUrl : '';
    this.editFrom = kind === 'offers' ? this.localDateTime((item as Offer).startsAtUtc)
      : kind === 'brochures' ? this.localDateTime((item as Brochure).validFromUtc) : '';
    this.editTo = this.localDateTime(kind === 'offers' ? (item as Offer).endsAtUtc
      : kind === 'brochures' ? (item as Brochure).validToUtc : (item as Job).expiresAtUtc);
    this.feedback('');
  }
  cancel(): void {
    if (this.busy) return;
    this.editKind = null;
    this.editId = '';
    this.cdr.markForCheck();
  }

  private secureUrl(raw: string): boolean {
    try {
      const value = new URL(raw);
      return value.protocol === 'https:' && Boolean(value.hostname) && !value.username && !value.password;
    } catch { return false; }
  }
  save(): void {
    const kind = this.editKind;
    if (this.busy || !kind || !this.editId || !this.token || !this.locationId) return;
    const title = this.editTitle.trim();
    const description = this.editDescription.trim();
    const url = this.editUrl.trim();
    const end = new Date(this.editTo);
    const start = new Date(this.editFrom);
    if (title.length < 3 || title.length > 120 || description.length > 2000) {
      this.feedback('Заглавието трябва да е между 3–120 знака; описанието — до 2000.', true);
      return;
    }
    if (!Number.isFinite(end.getTime()) || (this.editPublished && end.getTime() <= Date.now())) {
      this.feedback('Публикуваният материал се нуждае от валиден бъдещ краен срок.', true);
      return;
    }
    if (kind !== 'jobs' && (!Number.isFinite(start.getTime()) || start >= end
      || end.getTime() > start.getTime() + 2 * 366 * 86400000)) {
      this.feedback('Периодът е невалиден или над две години.', true);
      return;
    }
    if (kind !== 'offers' && !this.secureUrl(url)) {
      this.feedback('Външният URL трябва да използва HTTPS.', true);
      return;
    }
    if (kind === 'jobs' && (end.getTime() <= Date.now() || end.getTime() > Date.now() + 366 * 86400000)) {
      this.feedback('За обявата е необходим бъдещ срок до една година.', true);
      return;
    }
    const payload = kind === 'offers'
      ? { title, description, startsAtUtc: start.toISOString(), endsAtUtc: end.toISOString(), isPublished: this.editPublished }
      : kind === 'brochures'
        ? { title, url, validFromUtc: start.toISOString(), validToUtc: end.toISOString(), isPublished: this.editPublished }
        : { title, description, applyUrl: url, expiresAtUtc: end.toISOString(), isPublished: this.editPublished };

    const scope = this.locationId;
    this.busy = true;
    this.cdr.markForCheck();
    this.http.put(this.endpoint(kind, this.editId), payload, this.auth)
      .pipe(timeout(this.requestTimeout), takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.busy = false;
          if (scope !== this.locationId) return;
          this.cancel();
          this.feedback('Публикацията е записана.');
          this.reload();
        },
        error: reason => { this.busy = false; this.failure(reason); }
      });
  }
  changeVisibility(kind: Kind, item: BasePublication, action: 'hide' | 'restore'): void {
    if (this.busy || !this.token || !this.locationId) return;
    const scope = this.locationId;
    this.busy = true;
    this.cdr.markForCheck();
    this.http.post(this.endpoint(kind, item.id, action), {}, this.auth)
      .pipe(timeout(this.requestTimeout), takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.busy = false;
          if (scope !== this.locationId) return;
          if (this.editKind === kind && this.editId === item.id) {
            this.editPublished = action === 'restore';
          }
          this.feedback(action === 'hide' ? 'Публикацията е архивирана.' : 'Публикацията е възстановена.');
          this.reload();
        },
        error: reason => {
          this.busy = false;
          if (scope === this.locationId) this.failure(reason);
        }
      });
  }
}

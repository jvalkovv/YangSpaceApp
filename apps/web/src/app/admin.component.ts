import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { map, switchMap, timeout } from 'rxjs';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';

interface TokenResponse { accessToken: string; tokenType: string; expiresIn: number; refreshToken: string }
interface AdminLocation {
  id: string; slug: string; name: string; neighborhood: string; category: string;
  address: string; description: string; openingHours: string | null; isPublished: boolean;
  phone: string | null; website: string | null; imageUrl: string | null;
}
type LocationDraft = Omit<AdminLocation, 'id'>;
function blankLocation(): LocationDraft {
  return { slug: '', name: '', neighborhood: '', category: '', address: '',
    description: '', phone: null, website: null, imageUrl: null,
    openingHours: '', isPublished: false };
}
@Component({
  selector: 'ys-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="shell admin-shell">
      <span class="kicker">PREVIEW КОНЗОЛА</span><h1>Управление на квартала</h1>
      <p class="admin-lead">Само за изолирана тестова среда. Публична регистрация на търговци и реално модериране предстоят.</p>
      <div class="panel auth-panel" *ngIf="!token">
        <h2>Вход за администратор</h2>
        <p>Използвай акаунт, създаден чрез локалната bootstrap конфигурация (без пароли в Git).</p>
        <label>Имейл <input type="email" autocomplete="username" [(ngModel)]="email" /></label>
        <label>Парола <input type="password" autocomplete="current-password" [(ngModel)]="password" /></label>
        <button type="button" class="btn btn-primary" [disabled]="busy" (click)="login()">Вход</button>
      </div>
      <div *ngIf="token" class="admin-workspace">
        <div class="admin-actions"><strong>Администраторски режим · Preview</strong><button class="btn btn-outline" (click)="logout()">Изход</button></div>
        <div class="admin-columns">
          <section class="panel">
            <h2>Добави търговски обект</h2>
            <p class="admin-help">Новите обекти започват като скрита чернова. Публикувай след преглед.</p>
            <label>Име <input [(ngModel)]="draft.name" placeholder="Име на обект" /></label>
            <label>Slug (латиница и тирета) <input [(ngModel)]="draft.slug" placeholder="primeren-obekt" /></label>
            <label>Квартал <input [(ngModel)]="draft.neighborhood" placeholder="Цветен квартал" /></label>
            <label>Категория <input [(ngModel)]="draft.category" placeholder="Магазини" /></label>
            <label>Адрес <input [(ngModel)]="draft.address" placeholder="Улица, номер" /></label>
            <label>Описание <textarea [(ngModel)]="draft.description" rows="3"></textarea></label>
            <label>Работно време <input [(ngModel)]="draft.openingHours" placeholder="Пон–Пет: 09:00–18:00" /></label>
            <label>Телефон <input type="tel" [(ngModel)]="draft.phone" maxlength="40" placeholder="+359..." /></label>
            <label>Уебсайт (HTTPS) <input type="url" [(ngModel)]="draft.website" placeholder="https://..." /></label>
            <label>Изображение (HTTPS или /assets/) <input [(ngModel)]="draft.imageUrl" placeholder="https://..." /></label>
            <label class="checkline"><input type="checkbox" [(ngModel)]="draft.isPublished"> Публикувай веднага</label>
            <button type="button" class="btn btn-primary" [disabled]="busy" (click)="createLocation()">Запиши обект →</button>
          </section>
          <section class="panel">
            <h2>Публикации</h2>
            <label>Обект
              <select [(ngModel)]="selectedId"><option value="">Избери обект</option>
                <option *ngFor="let x of locations" [value]="x.id">{{x.name}}</option>
              </select>
            </label>
            <div class="admin-divider">Промоция</div>
            <label>Заглавие <input [(ngModel)]="offerTitle" placeholder="Седмична оферта" /></label>
            <label>Описание <textarea [(ngModel)]="offerDescription" rows="2"></textarea></label>
            <label>Начало <input type="datetime-local" [(ngModel)]="offerFrom" /></label>
            <label>Край <input type="datetime-local" [(ngModel)]="offerTo" /></label>
            <button class="btn btn-outline" [disabled]="busy || !selectedId" (click)="addOffer()">Публикувай оферта</button>
            <div class="admin-divider">Обява за работа</div>
            <label>Длъжност <input [(ngModel)]="jobTitle" placeholder="Продавач-консултант" /></label>
            <label>Описание <textarea [(ngModel)]="jobDescription" rows="2"></textarea></label>
            <label>URL за кандидатстване (HTTPS) <input [(ngModel)]="jobApplyUrl" placeholder="https://example.com/jobs" /></label>
            <button class="btn btn-outline" [disabled]="busy || !selectedId" (click)="addJob()">Публикувай позиция</button>
            <div class="admin-divider">Брошура (външен HTTPS URL)</div>
            <label>Заглавие <input [(ngModel)]="brochureTitle" placeholder="Седмична брошура" /></label>
            <label>PDF URL <input [(ngModel)]="brochureUrl" placeholder="https://example.com/file.pdf" /></label>
            <button class="btn btn-outline" [disabled]="busy || !selectedId" (click)="addBrochure()">Публикувай брошура</button>
          </section>
        </div>
        <section class="panel admin-list">
          <h2>Обекти ({{locations.length}})</h2>
          <div *ngFor="let x of locations; trackBy: trackLocation" class="admin-list-row">
            <div><strong>{{x.name}}</strong><small>{{x.neighborhood}} · {{x.category}} · {{x.isPublished ? 'Публичен' : 'Скрит'}}</small></div>
            <div class="admin-row-actions">
              <button type="button" class="text-button" [disabled]="busy" (click)="startEdit(x)">Редактирай</button>
              <button type="button" class="text-button" *ngIf="x.isPublished" [disabled]="busy" (click)="hide(x.id)">Скрий</button>
              <button type="button" class="text-button" *ngIf="!x.isPublished" [disabled]="busy" (click)="restore(x.id)">Публикувай</button>
            </div>
          </div>
        </section>
        <section class="panel admin-edit" *ngIf="editDraft as edit">
          <div class="admin-edit-header">
            <div><span class="kicker">РЕДАКЦИЯ НА ОБЕКТ</span><h2>{{edit.name || 'Без име'}}</h2></div>
            <button type="button" class="btn btn-outline" [disabled]="busy" (click)="cancelEdit()">Откажи</button>
          </div>
          <div class="admin-edit-fields">
            <label>Име <input [(ngModel)]="edit.name" maxlength="120" /></label>
            <label>Slug <input [(ngModel)]="edit.slug" maxlength="90" autocomplete="off" /></label>
            <label>Квартал <input [(ngModel)]="edit.neighborhood" maxlength="90" /></label>
            <label>Категория <input [(ngModel)]="edit.category" maxlength="80" /></label>
            <label>Адрес <input [(ngModel)]="edit.address" maxlength="240" /></label>
            <label>Телефон <input type="tel" [(ngModel)]="edit.phone" maxlength="40" /></label>
            <label>Уебсайт (HTTPS) <input type="url" [(ngModel)]="edit.website" /></label>
            <label>Изображение (HTTPS или /assets/) <input [(ngModel)]="edit.imageUrl" /></label>
            <label>Работно време <input [(ngModel)]="edit.openingHours" maxlength="250" /></label>
            <label class="admin-field-wide">Описание <textarea [(ngModel)]="edit.description" rows="4" maxlength="3000"></textarea></label>
          </div>
          <label class="checkline admin-publish-toggle">
            <input type="checkbox" [(ngModel)]="edit.isPublished" />
            Публичен профил (публикуване след запис)
          </label>
          <div class="admin-edit-actions">
            <button type="button" class="btn btn-primary" [disabled]="busy" (click)="saveEdit()">Запази промените</button>
            <span class="admin-help">Незаписаните промени не се показват публично.</span>
          </div>
        </section>
      </div>
      <div class="form-message" *ngIf="message" [class.error]="isError" [attr.role]="isError ? 'alert' : 'status'" aria-live="polite">{{message}}</div>
    </section>
  `
})
export class AdminComponent {
  private readonly http = inject(HttpClient);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly requestTimeout = 10000;
  private sessionVersion = 0;

  // Preview auth only: bearer stays in memory; never persisted in browser storage.
  token = '';
  email = '';
  password = '';
  busy = false;
  message = '';
  isError = false;

  locations: AdminLocation[] = [];
  selectedId = '';
  draft: LocationDraft = blankLocation();
  editDraft: LocationDraft | null = null;
  editingId = '';

  offerTitle = ''; offerDescription = ''; offerFrom = ''; offerTo = '';
  jobTitle = ''; jobDescription = ''; jobApplyUrl = '';
  brochureTitle = ''; brochureUrl = '';

  private get auth() {
    return { headers: new HttpHeaders({ Authorization: 'Bearer ' + this.token }) };
  }

  private status(message: string, isError = false): void {
    this.message = message;
    this.isError = isError;
    this.busy = false;
    this.cdr.markForCheck();
  }

  private fail(action: string, reason: unknown): void {
    const httpStatus = reason instanceof HttpErrorResponse ? reason.status : 0;
    if (httpStatus === 401 || httpStatus === 403) {
      this.logout();
      this.status('Сесията е невалидна или липсват администраторски права. Влез отново.', true);
      return;
    }
    if (httpStatus === 409) {
      this.status('Slug вече се използва. Избери уникален адрес за обекта.', true);
      return;
    }
    if (httpStatus === 0) {
      this.status(action + ': няма отговор от API.', true);
      return;
    }
    this.status(action + ' (HTTP ' + httpStatus + ').', true);
  }

  private prepare(input: LocationDraft): LocationDraft {
    return {
      slug: input.slug.trim(),
      name: input.name.trim(),
      neighborhood: input.neighborhood.trim(),
      category: input.category.trim(),
      address: input.address.trim(),
      description: input.description.trim(),
      phone: input.phone?.trim() || null,
      website: input.website?.trim() || null,
      imageUrl: input.imageUrl?.trim() || null,
      openingHours: input.openingHours?.trim() || null,
      isPublished: input.isPublished
    };
  }

  private validHttps(value: string | null): boolean {
    if (!value) return true;
    try {
      const url = new URL(value);
      return url.protocol === 'https:' && !url.username && !url.password;
    } catch { return false; }
  }

  private validate(input: LocationDraft): string | null {
    if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(input.slug) || input.slug.length > 90) {
      return 'Slug трябва да съдържа само малки латински букви, цифри и тирета (до 90 знака).';
    }
    if (input.name.length < 2 || input.name.length > 120 ||
      !input.neighborhood || input.neighborhood.length > 90 ||
      !input.category || input.category.length > 80 ||
      !input.address || input.address.length > 240) {
      return 'Провери името, квартала, категорията и адреса. Всички са задължителни.';
    }
    if (input.description.length > 3000 ||
      (input.phone?.length ?? 0) > 40 ||
      (input.openingHours?.length ?? 0) > 250) {
      return 'Описание, телефон или работно време надвишават ограниченията.';
    }
    if (!this.validHttps(input.website) ||
        (input.imageUrl && !(input.imageUrl.startsWith('/assets/') &&
          !input.imageUrl.includes('..')) && !this.validHttps(input.imageUrl))) {
      return 'Уебсайтът и изображението трябва да са HTTPS URL или изображението да е от /assets/.';
    }
    return null;
  }

  login(): void {
    if (this.busy) return;
    if (!this.email.trim() || !this.password) {
      this.status('Попълни имейл и парола.', true);
      return;
    }
    const attempt = ++this.sessionVersion;
    this.busy = true;
    this.message = '';
    this.cdr.markForCheck();
    this.http.post<TokenResponse>('/api/auth/login', {
      email: this.email.trim(), password: this.password
    }).pipe(
      timeout(this.requestTimeout),
      switchMap(response => {
        if (!response.accessToken) throw new Error('Missing access token');
        const headers = new HttpHeaders({ Authorization: 'Bearer ' + response.accessToken });
        return this.http.get<{ role: string }>('/api/v1/admin/me', { headers }).pipe(
          timeout(this.requestTimeout), map(identity => ({ response, identity }))
        );
      })
    ).subscribe({
      next: ({ response, identity }) => {
        if (attempt !== this.sessionVersion) return;
        this.password = '';
        if (identity.role !== 'PlatformAdmin') {
          this.status('Този акаунт няма права за администрация.', true);
          return;
        }
        this.token = response.accessToken;
        this.status('Администраторската роля е потвърдена.');
        this.reload();
      },
      error: () => {
        if (attempt !== this.sessionVersion) return;
        this.password = '';
        this.status('Входът е отказан или администраторската роля не е потвърдена.', true);
      }
    });
  }

  reload(): void {
    const expectedToken = this.token;
    if (!expectedToken) return;
    this.http.get<AdminLocation[]>('/api/v1/admin/locations', this.auth)
      .pipe(timeout(this.requestTimeout)).subscribe({
        next: items => {
          if (this.token !== expectedToken) return;
          this.locations = items;
          if (this.selectedId && !items.some(x => x.id === this.selectedId)) this.selectedId = '';
          this.cdr.markForCheck();
        },
        error: reason => {
          if (this.token !== expectedToken) return;
          this.fail('Списъкът с обекти не може да се зареди', reason);
        }
      });
  }

  createLocation(): void {
    if (this.busy || !this.token) return;
    const input = this.prepare(this.draft);
    const problem = this.validate(input);
    if (problem) { this.status(problem, true); return; }
    this.busy = true;
    this.http.post('/api/v1/admin/locations', input, this.auth)
      .pipe(timeout(this.requestTimeout)).subscribe({
        next: () => {
          this.draft = blankLocation();
          this.status('Обектът е създаден.');
          this.reload();
        },
        error: reason => this.fail('Обектът не е записан', reason)
      });
  }

  startEdit(location: AdminLocation): void {
    if (this.busy) return;
    this.editingId = location.id;
    this.editDraft = {
      slug: location.slug, name: location.name,
      neighborhood: location.neighborhood, category: location.category,
      address: location.address, description: location.description,
      phone: location.phone, website: location.website, imageUrl: location.imageUrl,
      openingHours: location.openingHours, isPublished: location.isPublished
    };
    this.message = '';
    this.cdr.markForCheck();
  }

  cancelEdit(): void {
    if (this.busy) return;
    this.editDraft = null;
    this.editingId = '';
    this.cdr.markForCheck();
  }

  saveEdit(): void {
    if (this.busy || !this.token || !this.editDraft || !this.editingId) return;
    const id = this.editingId;
    const input = this.prepare(this.editDraft);
    const problem = this.validate(input);
    if (problem) { this.status(problem, true); return; }
    this.busy = true;
    this.http.put('/api/v1/admin/locations/' + encodeURIComponent(id), input, this.auth)
      .pipe(timeout(this.requestTimeout)).subscribe({
        next: () => {
          this.editDraft = null;
          this.editingId = '';
          this.status('Промените са записани.');
          this.reload();
        },
        error: reason => this.fail('Промените не са записани', reason)
      });
  }

  private setPublication(id: string, action: 'hide' | 'restore'): void {
    if (this.busy || !this.token) return;
    this.busy = true;
    this.http.post('/api/v1/admin/locations/' + encodeURIComponent(id) + '/' + action, {}, this.auth)
      .pipe(timeout(this.requestTimeout)).subscribe({
        next: () => {
          if (this.editingId === id && this.editDraft) {
            this.editDraft.isPublished = action === 'restore';
          }
          this.status(action === 'restore' ? 'Обектът е публикуван.' : 'Обектът е скрит от публичния каталог.');
          this.reload();
        },
        error: reason => this.fail('Статусът не е променен', reason)
      });
  }

  hide(id: string): void { this.setPublication(id, 'hide'); }
  restore(id: string): void { this.setPublication(id, 'restore'); }
  trackLocation(_index: number, item: AdminLocation): string { return item.id; }

  addOffer(): void {
    if (!this.selectedId || !this.offerFrom || !this.offerTo || this.busy || !this.token) {
      this.status('Избери обект и период.', true);
      return;
    }
    const from = new Date(this.offerFrom);
    const to = new Date(this.offerTo);
    if (!Number.isFinite(from.getTime()) || !Number.isFinite(to.getTime()) || to <= from) {
      this.status('Невалиден период за офертата.', true);
      return;
    }
    this.busy = true;
    this.http.post('/api/v1/admin/locations/' + encodeURIComponent(this.selectedId) + '/offers', {
      title: this.offerTitle, description: this.offerDescription,
      startsAtUtc: from.toISOString(), endsAtUtc: to.toISOString(), isPublished: true
    }, this.auth).pipe(timeout(this.requestTimeout)).subscribe({
      next: () => this.status('Промоцията е публикувана.'),
      error: reason => this.fail('Промоцията не е записана', reason)
    });
  }

  addJob(): void {
    if (!this.selectedId || this.busy || !this.token) return;
    this.busy = true;
    this.http.post('/api/v1/admin/locations/' + encodeURIComponent(this.selectedId) + '/jobs', {
      title: this.jobTitle, description: this.jobDescription, applyUrl: this.jobApplyUrl,
      expiresAtUtc: new Date(Date.now() + 30 * 86400000).toISOString(), isPublished: true
    }, this.auth).pipe(timeout(this.requestTimeout)).subscribe({
      next: () => this.status('Обявата за работа е публикувана.'),
      error: reason => this.fail('Обявата не е записана', reason)
    });
  }

  addBrochure(): void {
    if (!this.selectedId || this.busy || !this.token) return;
    this.busy = true;
    this.http.post('/api/v1/admin/locations/' + encodeURIComponent(this.selectedId) + '/brochures', {
      title: this.brochureTitle, url: this.brochureUrl,
      validFromUtc: new Date(Date.now() - 3600000).toISOString(),
      validToUtc: new Date(Date.now() + 7 * 86400000).toISOString(), isPublished: true
    }, this.auth).pipe(timeout(this.requestTimeout)).subscribe({
      next: () => this.status('Брошурата е публикувана.'),
      error: reason => this.fail('Брошурата не е записана', reason)
    });
  }

  logout(): void {
    ++this.sessionVersion;
    this.token = '';
    this.password = '';
    this.locations = [];
    this.selectedId = '';
    this.editDraft = null;
    this.editingId = '';
    this.status('Излязохте.');
  }
}

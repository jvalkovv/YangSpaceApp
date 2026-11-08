import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';

interface TokenResponse { accessToken: string; tokenType: string; expiresIn: number; refreshToken: string }
interface AdminLocation {
  id: string; slug: string; name: string; neighborhood: string; category: string;
  address: string; description: string; openingHours: string | null; isPublished: boolean;
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
            <label>Име <input [(ngModel)]="draft.name" placeholder="Име на обект" /></label>
            <label>Slug (латиница и тирета) <input [(ngModel)]="draft.slug" placeholder="primeren-obekt" /></label>
            <label>Квартал <input [(ngModel)]="draft.neighborhood" placeholder="Цветен квартал" /></label>
            <label>Категория <input [(ngModel)]="draft.category" placeholder="Магазини" /></label>
            <label>Адрес <input [(ngModel)]="draft.address" placeholder="Улица, номер" /></label>
            <label>Описание <textarea [(ngModel)]="draft.description" rows="3"></textarea></label>
            <label>Работно време <input [(ngModel)]="draft.openingHours" placeholder="Пон–Пет: 09:00–18:00" /></label>
            <label class="checkline"><input type="checkbox" [(ngModel)]="draft.isPublished"> Публичен профил</label>
            <button class="btn btn-primary" [disabled]="busy" (click)="createLocation()">Добави обект →</button>
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
          <div *ngFor="let x of locations" class="admin-list-row">
            <div><strong>{{x.name}}</strong><small>{{x.neighborhood}} · {{x.category}} · {{x.isPublished ? 'Публичен' : 'Скрит'}}</small></div>
            <button class="text-button" *ngIf="x.isPublished" [disabled]="busy" (click)="hide(x.id)">Скрий</button>
          </div>
        </section>
      </div>
      <div class="form-message" *ngIf="message" [class.error]="isError" role="status">{{message}}</div>
    </section>
  `
})
export class AdminComponent {
  private readonly http = inject(HttpClient);
  private readonly cdr = inject(ChangeDetectorRef);
  // Preview auth only: token stays in memory; it is intentionally not stored in localStorage.
  token = ''; email = ''; password = ''; busy = false; message = ''; isError = false;
  locations: AdminLocation[] = []; selectedId = '';
  draft = { slug: '', name: '', neighborhood: '', category: '', address: '', description: '',
    phone: null, website: null, imageUrl: null, openingHours: '', isPublished: true };
  offerTitle = ''; offerDescription = ''; offerFrom = ''; offerTo = '';
  jobTitle = ''; jobDescription = ''; jobApplyUrl = '';
  brochureTitle = ''; brochureUrl = '';
  private get auth() { return { headers: new HttpHeaders({ Authorization: 'Bearer ' + this.token }) }; }
  private status(message: string, isError = false) { this.message = message; this.isError = isError; this.busy = false; this.cdr.markForCheck(); }
  login() {
    if (!this.email.trim() || !this.password) return this.status('Попълни имейл и парола.', true);
    this.busy = true;
    this.http.post<TokenResponse>('/api/auth/login', { email: this.email, password: this.password }).subscribe({
      next: response => { this.token = response.accessToken; this.password = ''; this.status('Влязохте в preview конзолата.'); this.reload(); },
      error: () => this.status('Входът е неуспешен или preview API не работи.', true)
    });
  }
  reload() {
    this.http.get<AdminLocation[]>('/api/v1/admin/locations', this.auth).subscribe({
      next: items => { if (!this.token) return; this.locations = items; this.cdr.markForCheck(); },
      error: () => this.status('Няма достъп до администраторските обекти.', true)
    });
  }
  createLocation() {
    this.busy = true;
    this.http.post('/api/v1/admin/locations', this.draft, this.auth).subscribe({
      next: () => { this.status('Обектът е записан.'); this.reload(); this.draft = { slug: '', name: '', neighborhood: '', category: '', address: '', description: '',
        phone: null, website: null, imageUrl: null, openingHours: '', isPublished: true }; },
      error: err => this.status('Грешка при запис (провери задължителните полета и уникалния slug). HTTP ' + err.status, true)
    });
  }
  addOffer() {
    if (!this.selectedId || !this.offerFrom || !this.offerTo) return this.status('Избери обект и период.', true);
    this.busy = true;
    this.http.post(`/api/v1/admin/locations/${this.selectedId}/offers`, {
      title: this.offerTitle, description: this.offerDescription,
      startsAtUtc: new Date(this.offerFrom).toISOString(), endsAtUtc: new Date(this.offerTo).toISOString(), isPublished: true
    }, this.auth).subscribe({
      next: () => this.status('Промоцията е публикувана.'),
      error: err => this.status('Промоцията не е записана. HTTP ' + err.status, true)
    });
  }
  addJob() {
    if (!this.selectedId) return;
    this.busy = true;
    this.http.post(`/api/v1/admin/locations/${this.selectedId}/jobs`, {
      title: this.jobTitle, description: this.jobDescription, applyUrl: this.jobApplyUrl,
      expiresAtUtc: new Date(Date.now() + 30 * 86400000).toISOString(), isPublished: true
    }, this.auth).subscribe({
      next: () => this.status('Обявата за работа е публикувана.'),
      error: err => this.status('Обявата не е записана. HTTP ' + err.status, true)
    });
  }
  addBrochure() {
    if (!this.selectedId) return;
    this.busy = true;
    this.http.post(`/api/v1/admin/locations/${this.selectedId}/brochures`, {
      title: this.brochureTitle, url: this.brochureUrl,
      validFromUtc: new Date(Date.now() - 3600000).toISOString(),
      validToUtc: new Date(Date.now() + 7 * 86400000).toISOString(), isPublished: true
    }, this.auth).subscribe({
      next: () => this.status('Брошурата е публикувана.'),
      error: err => this.status('Брошурата не е записана. HTTP ' + err.status, true)
    });
  }
  hide(id: string) {
    this.busy = true;
    this.http.post(`/api/v1/admin/locations/${id}/hide`, {}, this.auth).subscribe({
      next: () => { this.status('Обектът е скрит от публичния каталог.'); this.reload(); },
      error: err => this.status('Грешка при скриване. HTTP ' + err.status, true)
    });
  }
  logout() { this.token = ''; this.locations = []; this.password = ''; this.status('Излязохте.'); }
}

import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DirectoryApi, DirectoryMeta, LocationCard } from './directory-api.service';

@Component({
  selector: 'ys-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section class="hero">
      <div class="shell hero-inner">
        <div class="hero-copy">
          <div class="eyebrow"><span class="eyebrow-dot"></span> Локалното е по-близо, отколкото мислиш</div>
          <h1>Твоят квартал.<br><em>Всичко на едно място.</em></h1>
          <p>Магазини, услуги, промоции, брошури и работа около теб. Открий малките места с голямо значение.</p>
          <div class="hero-actions">
            <a href="#catalog" class="btn btn-light">Разгледай обектите <span aria-hidden="true">↗</span></a>
            <span class="subtle-hero">Пилотна версия · Варна</span>
          </div>
        </div>
        <div class="hero-art" aria-hidden="true">
          <div class="art-orbit orbit-one"></div><div class="art-orbit orbit-two"></div>
          <div class="art-center"><span>⌂</span><small>Всичко е близо</small></div>
          <div class="art-label label-top">☕ Кафе наблизо</div>
          <div class="art-label label-bottom">✦ Нови оферти</div>
        </div>
      </div>
    </section>
    <section class="shell catalog-section" id="catalog">
      <div class="section-intro">
        <div><span class="kicker">ОТКРИЙ</span><h2>Какво има около теб?</h2><p>Търси по квартал, тип обект или име.</p></div>
        <div class="demo-warning">ⓘ Показаните обекти са примерни.</div>
      </div>
      <form class="search-row" (ngSubmit)="load()">
        <label class="searchbox"><span aria-hidden="true">⌕</span><span class="sr-only">Търсене по име</span>
          <input name="q" type="search" [(ngModel)]="q" maxlength="100" placeholder="Пекарна, магазин, фризьор..." />
        </label>
        <label class="selectbox"><span class="sr-only">Квартал</span>
          <select name="neighborhood" [(ngModel)]="neighborhood">
            <option value="">Всички квартали</option>
            <option *ngFor="let n of meta.neighborhoods" [value]="n">{{n}}</option>
          </select>
        </label>
        <label class="selectbox"><span class="sr-only">Категория</span>
          <select name="category" [(ngModel)]="category">
            <option value="">Всички категории</option>
            <option *ngFor="let c of meta.categories" [value]="c">{{c}}</option>
          </select>
        </label>
        <button class="btn btn-primary" type="submit">Търси →</button>
      </form>
      <div class="results-heading"><strong>Места в твоя район</strong><span>{{ total }} обекта</span></div>
      <div *ngIf="loading" class="state-card">Зареждаме обектите…</div>
      <div *ngIf="error" class="state-card error" role="alert">{{error}} <button class="text-button" (click)="load()">Опитай пак</button></div>
      <div *ngIf="!loading && !error && locations.length === 0" class="state-card">Няма намерени обекти. Пробвай с други филтри.</div>
      <div class="locations-grid" *ngIf="!loading && !error">
        <a *ngFor="let item of locations" [routerLink]="['/obekt',item.slug]" class="location-card">
          <div class="card-cover" [class.peach]="item.category === 'Пекарни'" [class.mint]="item.category === 'Красота'">
            <div class="cover-glyph">{{icon(item.category)}}</div>
            <span class="cover-tag">{{item.category}}</span>
          </div>
          <div class="card-content">
            <div class="card-place">⌖ {{item.neighborhood}} · Варна</div>
            <h3>{{item.name}}</h3>
            <p>{{item.description}}</p>
            <div class="card-bottom">
              <span *ngIf="item.activeOffers" class="chip">✦ {{item.activeOffers}} предложения</span>
              <span *ngIf="item.activeJobs" class="chip chip-neutral">{{item.activeJobs}} работни позиции</span>
              <span class="card-arrow" aria-label="Виж подробности">↗</span>
            </div>
          </div>
        </a>
      </div>
      <div class="pagination" *ngIf="!loading && total > 12">
        <button class="btn btn-outline" type="button" [disabled]="page === 1" (click)="goPage(page - 1)">← Назад</button>
        <span>Страница {{page}} / {{pages}}</span>
        <button class="btn btn-outline" type="button" [disabled]="page >= pages" (click)="goPage(page + 1)">Напред →</button>
      </div>
    </section>
    <section class="merchant-section">
      <div class="shell merchant-inner">
        <div><span class="kicker">ЗА МЕСТНИЯ БИЗНЕС</span><h2>И твоят обект има място тук.</h2><p>Покажи услугите си на хората от квартала. Предстои отваряне на регистрацията за търговци.</p></div>
        <a routerLink="/admin" class="btn btn-light">Виж демо управлението ↗</a>
      </div>
    </section>
  `
})
export class HomeComponent implements OnInit {
  private readonly api = inject(DirectoryApi);
  locations: LocationCard[] = [];
  meta: DirectoryMeta = { cities: [], neighborhoods: [], categories: [] };
  q = ''; neighborhood = ''; category = '';
  page = 1; total = 0; loading = false; error = '';
  get pages() { return Math.max(1, Math.ceil(this.total / 12)); }
  ngOnInit(): void {
    this.api.meta().subscribe({ next: value => this.meta = value, error: () => {} });
    this.load();
  }
  load(): void {
    this.loading = true; this.error = '';
    this.api.search({ q: this.q, neighborhood: this.neighborhood, category: this.category, page: this.page })
      .subscribe({
        next: value => { this.locations = value.items; this.total = value.total; this.loading = false; },
        error: () => { this.error = 'Не успяхме да заредим справочника. Провери дали API работи.'; this.loading = false; }
      });
  }
  goPage(n: number) { this.page = n; this.load(); document.getElementById('catalog')?.scrollIntoView({ behavior: 'smooth' }); }
  icon(category: string): string {
    const icons: Record<string, string> = { 'Пекарни': '🥐', 'Красота': '✂', 'Хранителни магазини': '🛍', 'Заведения': '☕', 'Услуги': '⚙', 'Магазини': '✦' };
    return icons[category] ?? '⌂';
  }
}

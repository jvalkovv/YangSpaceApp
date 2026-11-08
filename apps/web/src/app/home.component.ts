import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, Subscription, debounceTime } from 'rxjs';
import {
  DirectoryApi, DirectoryMeta, HomepagePayload, LocationCard, PublicHighlight
} from './directory-api.service';

@Component({
  selector: 'ys-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section class="hero" aria-labelledby="hero-title">
      <div class="hero-glow" aria-hidden="true"></div>
      <div class="shell hero-inner">
        <div class="hero-copy">
          <span class="hero-kicker"><span class="status-dot"></span> Варна · локалната общност</span>
          <h1 id="hero-title">Добрите места са <span>по-близо,</span> отколкото мислиш.</h1>
          <p class="hero-lead">Любими магазини, местни услуги, актуални предложения и работа — всичко важно за твоя квартал на едно място.</p>
          <div class="hero-actions">
            <a href="#catalog" class="btn btn-cream">Разгледай обектите <span aria-hidden="true">↗</span></a>
            <a href="#offers" class="hero-text-link">Виж предложенията <span aria-hidden="true">→</span></a>
          </div>
          <div class="hero-points" aria-label="За какво е YangSpace">
            <span><span aria-hidden="true">◈</span> Места около теб</span>
            <span><span aria-hidden="true">✳</span> Актуални предложения</span>
            <span><span aria-hidden="true">⌁</span> Нови възможности</span>
          </div>
        </div>
        <div class="hero-visual" aria-hidden="true">
          <div class="neighborhood-map">
            <span class="map-label top-label">В твоя квартал</span>
            <span class="map-street street-a"></span><span class="map-street street-b"></span>
            <span class="map-street street-c"></span><span class="map-street street-d"></span>
            <span class="map-dot dot-a">☕</span>
            <span class="map-dot dot-b">✿</span>
            <span class="map-dot dot-c">⌂</span>
            <span class="map-dot dot-d">✦</span>
            <div class="map-feature">
              <span class="map-feature-icon">⌖</span>
              <div><strong>Открий наблизо</strong><small>Места, оферти и хора</small></div>
            </div>
            <span class="map-label bottom-label">Близо е. Открий го.</span>
          </div>
          <div class="floating-card floating-left"><span class="floating-symbol">✦</span><div><strong>Нови предложения</strong><small>От бизнеса около теб</small></div></div>
          <div class="floating-card floating-right"><span class="floating-symbol warm">⌂</span><div><strong>Местни обекти</strong><small>На една ръка разстояние</small></div></div>
        </div>
      </div>
    </section>

    <div class="preview-strip">
      <div class="shell preview-strip-inner">
        <span><strong>DEMO PREVIEW</strong> · Платформата е в разработка, а показаните обекти и предложения са примерни.</span>
        <a href="#how-it-works">Как работи <span aria-hidden="true">→</span></a>
      </div>
    </div>

    <section class="shell quick-discovery" aria-labelledby="discovery-title">
      <div class="section-heading compact">
        <div><span class="kicker">ЛЕСНО ОТКРИВАНЕ</span><h2 id="discovery-title">Кое ти трябва днес?</h2></div>
        <p>Избери категория и разгледай местата наоколо.</p>
      </div>
      <div class="category-chips category-loading" *ngIf="loading && meta.categories.length === 0" aria-hidden="true">
        <span *ngFor="let i of [1,2,3,4,5]" class="category-placeholder shimmer"></span>
      </div>
      <div class="category-chips" aria-label="Категории обекти">
        <button type="button" class="category-chip" [class.selected]="!category"
                [attr.aria-pressed]="!category" (click)="chooseCategory('')">
          <span class="category-icon">⌂</span> Всички места
        </button>
        <button type="button" class="category-chip" *ngFor="let cat of meta.categories; trackBy: trackCategory"
                [class.selected]="category === cat" [attr.aria-pressed]="category === cat"
                (click)="chooseCategory(cat)">
          <span class="category-icon">{{ icon(cat) }}</span> {{cat}}
        </button>
      </div>
    </section>

    <section class="catalog-wrap" id="catalog" aria-labelledby="catalog-title">
      <div class="shell">
        <div class="section-heading">
          <div><span class="kicker">ЛОКАЛЕН КАТАЛОГ</span><h2 id="catalog-title">Места, които си струва да откриеш.</h2><p>Търси по име, квартал или категория. Без задължителна регистрация.</p></div>
          <span class="section-index">01 / ОТКРИЙ</span>
        </div>
        <form class="search-panel" (ngSubmit)="submitSearch()" role="search" aria-label="Търсене на търговски обекти">
          <label class="search-field search-main">
            <span class="search-icon" aria-hidden="true">⌕</span>
            <span class="sr-only">Име на обект или ключова дума</span>
            <input type="search" name="q" [(ngModel)]="q" (ngModelChange)="queueSearch()"
                   placeholder="Магазин, кафе, фризьор..." maxlength="100" autocomplete="off" />
          </label>
          <label class="search-field">
            <span class="sr-only">Квартал</span>
            <select name="neighborhood" [(ngModel)]="neighborhood" (ngModelChange)="filterChanged()" aria-label="Избери квартал">
              <option value="">Всички квартали</option>
              <option *ngFor="let item of meta.neighborhoods" [value]="item">{{item}}</option>
            </select>
          </label>
          <label class="search-field">
            <span class="sr-only">Категория</span>
            <select name="category" [(ngModel)]="category" (ngModelChange)="filterChanged()" aria-label="Избери категория">
              <option value="">Всички категории</option>
              <option *ngFor="let item of meta.categories" [value]="item">{{item}}</option>
            </select>
          </label>
          <button class="btn btn-primary search-submit" type="submit">Търси <span aria-hidden="true">→</span></button>
        </form>

        <div class="catalog-toolbar">
          <div><strong>{{ loading && !hasLoaded ? 'Откриваме местата…' : 'Обекти в твоя район' }}</strong>
            <span class="results-count" *ngIf="hasLoaded">{{total}} {{total === 1 ? 'обект' : 'обекта'}}</span>
          </div>
          <button type="button" class="reset-button" *ngIf="hasFilters" (click)="resetFilters()">Изчисти филтрите ×</button>
        </div>

        <div role="status" class="update-status" *ngIf="loading && hasLoaded">Обновяваме резултатите…</div>
        <div *ngIf="error" class="state-card error-state" role="alert">
          <span class="state-icon">!</span>
          <div><strong>Не успяхме да заредим обектите.</strong><p>{{error}}</p></div>
          <button type="button" class="btn btn-outline" (click)="reload()">Опитай отново</button>
        </div>

        <div class="locations-grid skeleton-grid" *ngIf="loading && !hasLoaded" aria-label="Зареждане на каталога" aria-busy="true">
          <div class="location-card skeleton-card" *ngFor="let i of skeletons">
            <div class="skeleton-cover shimmer"></div>
            <div class="card-content"><div class="skeleton-line shimmer short"></div><div class="skeleton-line shimmer medium"></div><div class="skeleton-line shimmer"></div></div>
          </div>
        </div>

        <div class="state-card empty-state" *ngIf="!loading && !error && locations.length === 0">
          <span class="state-icon">⌕</span>
          <div><strong>Няма резултати за тези филтри.</strong><p>Пробвай с друг квартал или категория.</p></div>
          <button type="button" class="btn btn-outline" (click)="resetFilters()">Покажи всички</button>
        </div>

        <div class="locations-grid" *ngIf="hasLoaded && locations.length > 0" [class.results-refreshing]="loading" aria-label="Намерени търговски обекти">
          <a *ngFor="let item of locations; trackBy: trackLocation" [routerLink]="['/obekt', item.slug]" class="location-card">
            <div class="card-cover" [ngClass]="cover(item.category)">
              <span class="cover-pattern" aria-hidden="true"></span>
              <span class="cover-symbol" aria-hidden="true">{{icon(item.category)}}</span>
              <span class="cover-category">{{item.category}}</span>
              <span class="cover-corner" aria-hidden="true">↗</span>
            </div>
            <div class="card-content">
              <span class="card-location"><span aria-hidden="true">⌖</span> {{item.neighborhood}} · Варна</span>
              <h3>{{item.name}}</h3>
              <p>{{item.description}}</p>
              <div class="card-bottom">
                <span class="mini-tag" *ngIf="item.activeOffers > 0">✳ {{item.activeOffers}} {{item.activeOffers === 1 ? 'оферта' : 'оферти'}}</span>
                <span class="mini-tag neutral" *ngIf="item.activeJobs > 0">⌁ {{item.activeJobs}} {{item.activeJobs === 1 ? 'позиция' : 'позиции'}}</span>
                <span class="card-more">Разгледай <span aria-hidden="true">→</span></span>
              </div>
            </div>
          </a>
        </div>

        <nav class="pagination" *ngIf="!loading && total > 12" aria-label="Страници на каталога">
          <button type="button" class="btn btn-outline" [disabled]="page === 1" (click)="changePage(page-1)">← Предишна</button>
          <span>Страница {{page}} от {{pages}}</span>
          <button type="button" class="btn btn-outline" [disabled]="page === pages" (click)="changePage(page+1)">Следваща →</button>
        </nav>
      </div>
    </section>

    <section class="offers-section" id="offers" aria-labelledby="offers-title">
      <div class="shell">
        <div class="section-heading">
          <div><span class="kicker">БЛИЗО ДО ТЕБ</span><h2 id="offers-title">Повече от обикновен каталог.</h2><p>Оферти, брошури и нови възможности на едно място.</p></div>
          <span class="section-index">02 / ВЪЗМОЖНОСТИ</span>
        </div>
        <div class="discover-grid">
          <article class="discover-card discover-offers">
            <div class="discover-card-top"><span class="discover-icon">✳</span><span class="discover-number">01</span></div>
            <h3>Актуални оферти</h3><p class="discover-intro">Виж какво предлагат обектите близо до теб.</p>
            <div class="discover-items" *ngIf="offers.length; else noOffers">
              <a *ngFor="let item of offers; trackBy: trackHighlight" [routerLink]="['/obekt',item.slug]" class="discover-item">
                <span><strong>{{item.title}}</strong><small>{{item.locationName}} · {{item.neighborhood}}</small></span><span aria-hidden="true">↗</span>
              </a>
            </div>
            <ng-template #noOffers><div class="discover-empty">{{loadingExtra ? 'Зареждаме офертите…' : 'Няма активни предложения в момента.'}}</div></ng-template>
          </article>
          <article class="discover-card discover-brochures">
            <div class="discover-card-top"><span class="discover-icon">▤</span><span class="discover-number">02</span></div>
            <h3>Брошури и каталози</h3><p class="discover-intro">Седмични и месечни издания от местния бизнес.</p>
            <div class="discover-items" *ngIf="brochures.length; else noBrochures">
              <a *ngFor="let item of brochures; trackBy: trackHighlight" [routerLink]="['/obekt',item.slug]" class="discover-item">
                <span><strong>{{item.title}}</strong><small>{{item.locationName}} · {{item.neighborhood}}</small></span><span aria-hidden="true">↗</span>
              </a>
            </div>
            <ng-template #noBrochures><div class="discover-empty">{{loadingExtra ? 'Зареждаме брошурите…' : 'Няма активни брошури в момента.'}}</div></ng-template>
          </article>
          <article class="discover-card discover-jobs" id="jobs">
            <div class="discover-card-top"><span class="discover-icon">⌁</span><span class="discover-number">03</span></div>
            <h3>Работа наблизо</h3><p class="discover-intro">Възможности в квартала, без да търсиш надалеч.</p>
            <div class="discover-items" *ngIf="jobs.length; else noJobs">
              <a *ngFor="let item of jobs; trackBy: trackHighlight" [routerLink]="['/obekt',item.slug]" class="discover-item">
                <span><strong>{{item.title}}</strong><small>{{item.locationName}} · {{item.neighborhood}}</small></span><span aria-hidden="true">↗</span>
              </a>
            </div>
            <ng-template #noJobs><div class="discover-empty">{{loadingExtra ? 'Зареждаме позициите…' : 'Няма активни позиции в момента.'}}</div></ng-template>
          </article>
        </div>
        <p class="demo-footnote">Показаните предложения са демонстрационни. Реални публикации ще има след пилотното включване на търговци.</p>
      </div>
    </section>

    <section class="how-section" id="how-it-works" aria-labelledby="how-title">
      <div class="shell">
        <div class="section-heading centered">
          <div><span class="kicker">СЪЗДАДЕНО ЗА ТВОЯ КВАРТАЛ</span><h2 id="how-title">Малките открития правят деня по-добър.</h2><p>Местните бизнеси и хората се срещат по най-лесния начин.</p></div>
        </div>
        <div class="steps-grid">
          <div class="step-item"><span class="step-number">01</span><div class="step-illustration">⌖</div><h3>Избери квартал</h3><p>Намери местата, услугите и търговците около теб.</p></div>
          <div class="step-item"><span class="step-number">02</span><div class="step-illustration">✦</div><h3>Открий повече</h3><p>Разгледай актуални обяви, предложения и информация.</p></div>
          <div class="step-item"><span class="step-number">03</span><div class="step-illustration">↗</div><h3>Свържи се директно</h3><p>Посети обекта, разгледай сайта или кандидатствай за работа.</p></div>
        </div>
      </div>
    </section>

    <section class="business-section" id="business" aria-labelledby="business-title">
      <div class="shell business-inner">
        <div class="business-mark" aria-hidden="true">Y.</div>
        <div class="business-copy"><span class="kicker">ЗА МЕСТНИЯ БИЗНЕС</span>
          <h2 id="business-title">Твоят бизнес заслужава да бъде открит.</h2>
          <p>Представи обекта си, покажи предложенията и стигни до хората от твоя квартал. Регистрацията за търговци предстои.</p>
        </div>
        <div class="business-action">
          <a routerLink="/admin" class="btn btn-cream">Виж демо управлението <span aria-hidden="true">↗</span></a>
          <span>Административен preview · не е публична регистрация</span>
        </div>
      </div>
    </section>
  `
})
export class HomeComponent implements OnInit {
  private readonly api = inject(DirectoryApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly pendingText = new Subject<number>();
  private searchSubscription?: Subscription;
  private requestVersion = 0;
  private pendingTextVersion = 0;

  readonly skeletons = [1, 2, 3, 4, 5, 6];
  locations: LocationCard[] = [];
  meta: DirectoryMeta = { cities: [], neighborhoods: [], categories: [] };
  offers: PublicHighlight[] = [];
  brochures: PublicHighlight[] = [];
  jobs: PublicHighlight[] = [];
  q = '';
  neighborhood = '';
  category = '';
  page = 1;
  total = 0;
  loading = true;
  loadingExtra = true;
  hasLoaded = false;
  error = '';

  constructor() {
    this.pendingText.pipe(debounceTime(280), takeUntilDestroyed()).subscribe(version => {
      if (version === this.pendingTextVersion) this.performSearch();
    });
    this.destroyRef.onDestroy(() => this.searchSubscription?.unsubscribe());
  }

  ngOnInit(): void {
    this.api.homepage().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (payload: HomepagePayload) => {
        this.meta = payload.meta;
        this.offers = payload.offers;
        this.brochures = payload.brochures;
        this.jobs = payload.jobs;
        this.loadingExtra = false;
        // Do not overwrite results if the visitor searched before bootstrap finished.
        if (this.requestVersion === 0) this.acceptResults(payload.directory);
      },
      error: (reason: unknown) => {
        this.loadingExtra = false;
        // A 404 indicates a temporarily older API; search can still be used.
        // A timeout/network error must NOT trigger another 10-second wait.
        if (reason instanceof HttpErrorResponse && reason.status === 404) {
          this.api.meta().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: m => this.meta = m,
            error: () => {}
          });
          if (this.requestVersion === 0) this.performSearch();
          return;
        }
        if (this.requestVersion === 0) {
          this.loading = false;
          this.hasLoaded = true;
          this.error = 'Не можем да се свържем с каталога. Провери дали API работи на 127.0.0.1:5090 и опитай отново.';
        }
      }
    });
  }

  get pages(): number { return Math.max(1, Math.ceil(this.total / 12)); }
  get hasFilters(): boolean { return !!(this.q.trim() || this.neighborhood || this.category); }

  private acceptResults(data: { items: LocationCard[]; total: number; page: number }): void {
    this.locations = data.items;
    this.total = data.total;
    this.page = data.page;
    this.loading = false;
    this.hasLoaded = true;
    this.error = '';
  }

  queueSearch(): void {
    this.page = 1;
    this.pendingText.next(++this.pendingTextVersion);
  }

  submitSearch(): void {
    ++this.pendingTextVersion;
    this.page = 1;
    this.performSearch();
  }

  filterChanged(): void {
    ++this.pendingTextVersion;
    this.page = 1;
    this.performSearch();
  }

  chooseCategory(value: string): void {
    ++this.pendingTextVersion;
    this.category = value;
    this.page = 1;
    this.performSearch();
  }

  resetFilters(): void {
    ++this.pendingTextVersion;
    this.q = '';
    this.category = '';
    this.neighborhood = '';
    this.page = 1;
    this.performSearch();
  }

  reload(): void { ++this.pendingTextVersion; this.performSearch(); }

  changePage(page: number): void {
    ++this.pendingTextVersion;
    if (page < 1 || page > this.pages || page === this.page) return;
    this.page = page;
    this.performSearch();
  }

  private performSearch(): void {
    const version = ++this.requestVersion;
    this.searchSubscription?.unsubscribe();
    this.loading = true;
    this.error = '';
    this.searchSubscription = this.api.search({
      q: this.q.trim(), category: this.category, neighborhood: this.neighborhood, page: this.page
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => { if (version === this.requestVersion) this.acceptResults(result); },
      error: () => {
        if (version !== this.requestVersion) return;
        this.loading = false;
        this.error = 'API заявката не завърши успешно. Провери дали .NET приложението работи на 127.0.0.1:5090 и опитай пак.';
      }
    });
  }

  trackLocation(_index: number, item: LocationCard): string { return item.id; }
  trackHighlight(_index: number, item: PublicHighlight): string { return item.slug + item.title; }
  trackCategory(_index: number, item: string): string { return item; }

  icon(category: string): string {
    const icons: Record<string, string> = {
      'Пекарни': '🥐', 'Красота': '✿', 'Хранителни магазини': '◈',
      'Заведения': '☕', 'Услуги': '⚙', 'Магазини': '▧'
    };
    return icons[category] ?? '⌂';
  }
  cover(category: string): string {
    const styles: Record<string, string> = {
      'Пекарни': 'cover-sand', 'Красота': 'cover-blush',
      'Хранителни магазини': 'cover-lime', 'Заведения': 'cover-coffee',
      'Услуги': 'cover-slate', 'Магазини': 'cover-lilac'
    };
    return styles[category] ?? 'cover-sage';
  }
}

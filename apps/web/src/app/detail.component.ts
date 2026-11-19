import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Title } from '@angular/platform-browser';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  combineLatest, catchError, distinctUntilChanged, map, of, startWith, Subject, switchMap
} from 'rxjs';
import { DirectoryApi, LocationDetail, describeDirectoryFailure } from './directory-api.service';

// Public timestamps are consistently shown in Bulgaria's civil timezone,
// including daylight-saving transitions, regardless of browser timezone.
const bulgarianDateTime = new Intl.DateTimeFormat('bg-BG', {
  timeZone: 'Europe/Sofia',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23'
});

type DetailState = 'loading' | 'success' | 'not-found' | 'unavailable';
type DetailLoadResult =
  | { kind: 'loaded'; location: LocationDetail }
  | { kind: 'missing' }
  | { kind: 'failed'; notFound: boolean; message: string };

@Component({
  selector: 'ys-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="shell detail-shell" aria-labelledby="detail-page-title" [attr.aria-busy]="state === 'loading'">
      <a routerLink="/" class="backlink detail-backlink">
        <span aria-hidden="true">←</span> Към всички обекти
      </a>

      <div class="detail-state-card detail-loading-card detail-loading" *ngIf="state === 'loading'"
           role="status" aria-live="polite" aria-busy="true">
        <span class="detail-state-icon" aria-hidden="true"><span class="detail-spinner"></span></span>
        <div class="detail-state-copy">
          <span class="kicker">ПРОФИЛ НА ОБЕКТ</span>
          <h1 id="detail-page-title">Зареждаме информацията…</h1>
          <p>Подготвяме контактите, актуалните предложения и подробностите за обекта.</p>
          <div class="detail-loading-lines" aria-hidden="true">
            <span></span><span></span><span></span>
          </div>
        </div>
      </div>

      <div class="detail-state-card detail-not-found-card" *ngIf="state === 'not-found'"
           role="alert" aria-live="assertive">
        <span class="detail-state-icon" aria-hidden="true">⌕</span>
        <div class="detail-state-copy">
          <span class="kicker">ОБЕКТЪТ НЕ Е НАЛИЧЕН</span>
          <h1 id="detail-page-title">Не открихме този обект.</h1>
          <p>{{errorMessage}}</p>
          <a routerLink="/" class="btn btn-primary">Към каталога <span aria-hidden="true">→</span></a>
        </div>
      </div>

      <div class="detail-state-card detail-error-card" *ngIf="state === 'unavailable'"
           role="alert" aria-live="assertive">
        <span class="detail-state-icon" aria-hidden="true">!</span>
        <div class="detail-state-copy">
          <span class="kicker">ВРЕМЕНЕН ПРОБЛЕМ</span>
          <h1 id="detail-page-title">В момента не можем да заредим профила.</h1>
          <p>{{errorMessage}}</p>
          <div class="detail-state-actions">
            <button type="button" class="btn btn-primary" (click)="retry()">Опитай отново <span aria-hidden="true">↻</span></button>
            <a routerLink="/" class="btn btn-outline">Към каталога</a>
          </div>
        </div>
      </div>

      <ng-container *ngIf="location as item">
        <header class="detail-hero">
          <div class="detail-hero-media" *ngIf="item.imageUrl; else detailFallback">
            <img [src]="item.imageUrl" [alt]="'Снимка на ' + item.name" fetchpriority="high" />
          </div>
          <ng-template #detailFallback>
            <div class="detail-icon" aria-hidden="true">{{icon(item.category)}}</div>
          </ng-template>

          <div class="detail-hero-copy">
            <div class="detail-breadcrumb">
              <span>{{item.category}}</span><span aria-hidden="true">·</span><span>{{item.neighborhood}}</span>
            </div>
            <h1 id="detail-page-title">{{item.name}}</h1>
            <p>{{item.description}}</p>
            <span class="profile-status" [class.profile-status-verified]="item.isVerified">
              <span aria-hidden="true">{{item.isVerified ? '✓' : 'i'}}</span>
              {{item.isVerified ? 'Проверен профил' : 'Информацията не е потвърдена'}}
            </span>
          </div>
        </header>

        <div class="detail-grid">
          <div class="detail-main">
            <section class="panel detail-content-panel" aria-labelledby="detail-offers-title">
              <div class="detail-panel-heading">
                <div><span class="kicker">АКТУАЛНО</span><h2 id="detail-offers-title">Предложения</h2></div>
                <span class="detail-count">{{item.offers.length}}</span>
              </div>
              <div class="detail-empty" *ngIf="!item.offers.length" role="status">
                <span aria-hidden="true">✳</span><div><strong>Няма активни предложения.</strong><p>Когато обектът публикува нова оферта, ще я откриеш тук.</p></div>
              </div>
              <article class="offer-line detail-entry" *ngFor="let o of item.offers">
                <div class="detail-entry-heading"><strong>{{o.title}}</strong><span class="detail-entry-tag">ОФЕРТА</span></div>
                <p>{{o.description}}</p>
                <span class="detail-entry-date">Офертата е валидна от <time [attr.datetime]="o.startsAtUtc">{{formatBulgarianDateTime(o.startsAtUtc)}}</time>
                  до <time [attr.datetime]="o.endsAtUtc">{{formatBulgarianDateTime(o.endsAtUtc)}}</time> · българско време</span>
              </article>
            </section>

            <section class="panel detail-content-panel" aria-labelledby="detail-brochures-title">
              <div class="detail-panel-heading">
                <div><span class="kicker">РАЗГЛЕДАЙ</span><h2 id="detail-brochures-title">Брошури и каталози</h2></div>
                <span class="detail-count">{{item.brochures.length}}</span>
              </div>
              <div class="detail-empty" *ngIf="!item.brochures.length" role="status">
                <span aria-hidden="true">▤</span><div><strong>Няма активни брошури.</strong><p>Публикуваните каталози ще се показват в този раздел.</p></div>
              </div>
              <article class="offer-line detail-entry" *ngFor="let b of item.brochures">
                <div class="detail-entry-heading"><strong>{{b.title}}</strong><span class="detail-entry-tag">БРОШУРА</span></div>
                <p>Валидност на брошурата:
                  <time [attr.datetime]="b.validFromUtc">{{formatBulgarianDateTime(b.validFromUtc)}}</time> –
                  <time [attr.datetime]="b.validToUtc">{{formatBulgarianDateTime(b.validToUtc)}}</time> · българско време</p>
                <a class="detail-entry-link" [href]="b.url" rel="noopener noreferrer" target="_blank">Отвори брошурата <span aria-hidden="true">↗</span></a>
              </article>
            </section>

            <section class="panel detail-content-panel" aria-labelledby="detail-jobs-title">
              <div class="detail-panel-heading">
                <div><span class="kicker">ВЪЗМОЖНОСТИ</span><h2 id="detail-jobs-title">Работа в обекта</h2></div>
                <span class="detail-count">{{item.jobs.length}}</span>
              </div>
              <div class="detail-empty" *ngIf="!item.jobs.length" role="status">
                <span aria-hidden="true">⌁</span><div><strong>В момента няма активни позиции.</strong><p>Новите възможности за работа ще се появяват тук.</p></div>
              </div>
              <article class="offer-line detail-entry" *ngFor="let j of item.jobs">
                <div class="detail-entry-heading"><strong>{{j.title}}</strong><span class="detail-entry-tag">РАБОТА</span></div>
                <p>{{j.description}}</p>
                <span class="detail-entry-date">Обявата е активна до <time [attr.datetime]="j.expiresAtUtc">{{formatBulgarianDateTime(j.expiresAtUtc)}}</time> · българско време</span>
                <a class="detail-entry-link" [href]="j.applyUrl" rel="noopener noreferrer" target="_blank">Научи повече <span aria-hidden="true">↗</span></a>
              </article>
            </section>
          </div>

          <aside class="detail-sidebar panel" aria-labelledby="detail-info-title">
            <span class="kicker">НАМЕРИ НИ</span>
            <h2 id="detail-info-title">Информация за обекта</h2>
            <div class="detail-info"><span>Локация</span><strong>{{item.neighborhood}}, {{item.city}}</strong></div>
            <div class="detail-info"><span>Адрес</span><strong>{{item.address}}</strong></div>
            <div class="detail-info"><span>Работно време</span><strong>{{item.openingHours || 'Не е посочено'}}</strong></div>
            <div class="detail-info" *ngIf="item.phone"><span>Телефон</span><a [href]="'tel:' + item.phone">{{item.phone}}</a></div>
            <div class="detail-info" *ngIf="item.website"><span>Уебсайт</span><a [href]="item.website" target="_blank" rel="noopener noreferrer">Посети сайта ↗</a></div>
            <div class="detail-contact-actions" *ngIf="item.phone || item.website">
              <a *ngIf="item.phone" class="btn btn-primary" [href]="'tel:' + item.phone">Обади се <span aria-hidden="true">↗</span></a>
              <a *ngIf="item.website" class="btn btn-outline" [href]="item.website" target="_blank" rel="noopener noreferrer">Уебсайт <span aria-hidden="true">↗</span></a>
            </div>
            <small class="profile-updated">Профилът е редактиран на
              <time [attr.datetime]="item.updatedAtUtc">{{formatBulgarianDateTime(item.updatedAtUtc)}}</time> · българско време</small>
          </aside>
        </div>
      </ng-container>
    </section>
  `
})
export class DetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(DirectoryApi);
  private readonly title = inject(Title);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly retry$ = new Subject<void>();
  private currentSlug: string | null = null;

  location: LocationDetail | null = null;
  state: DetailState = 'loading';
  errorMessage = '';

  formatBulgarianDateTime(value: string): string {
    const date = new Date(value);
    return Number.isFinite(date.getTime()) ? bulgarianDateTime.format(date) : 'Няма данни';
  }

  icon(category: string): string {
    const normalized = category.toLocaleLowerCase('bg-BG');
    if (normalized.includes('завед') || normalized.includes('каф')) return '☕';
    if (normalized.includes('услуг')) return '✦';
    if (normalized.includes('магаз') || normalized.includes('хран')) return '⌂';
    if (normalized.includes('крас')) return '✿';
    return '⌖';
  }

  retry(): void {
    if (this.state === 'unavailable' && this.currentSlug) {
      this.retry$.next();
    }
  }

  ngOnInit(): void {
    const slug$ = this.route.paramMap.pipe(
      map(params => params.get('slug')?.trim() || null),
      distinctUntilChanged()
    );

    combineLatest([slug$, this.retry$.pipe(startWith(undefined))]).pipe(
      switchMap(([slug]) => {
        this.currentSlug = slug;
        this.location = null;
        this.errorMessage = '';
        this.state = 'loading';
        this.title.setTitle('Зареждане на обект | YangSpace');
        this.cdr.markForCheck();

        if (!slug) return of({ kind: 'missing' } as DetailLoadResult);

        return this.api.location(slug).pipe(
          map(location => ({ kind: 'loaded', location }) as DetailLoadResult),
          catchError((reason: unknown) => of({
            kind: 'failed',
            notFound: reason instanceof HttpErrorResponse && reason.status === 404,
            message: describeDirectoryFailure(reason)
          } as DetailLoadResult))
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(result => {
      if (result.kind === 'loaded') {
        this.location = result.location;
        this.state = 'success';
        this.title.setTitle(result.location.name + ' | YangSpace');
      } else if (result.kind === 'missing') {
        this.state = 'not-found';
        this.errorMessage = 'Липсва идентификатор на обекта. Върни се към каталога и избери обект от списъка.';
        this.title.setTitle('Обектът не е намерен | YangSpace');
      } else if (result.notFound) {
        this.state = 'not-found';
        this.errorMessage = 'Обектът не е намерен или е скрит.';
        this.title.setTitle('Обектът не е намерен | YangSpace');
      } else {
        this.state = 'unavailable';
        this.errorMessage = result.message;
        this.title.setTitle('Временен проблем | YangSpace');
      }

      this.cdr.markForCheck();
    });
  }
}

import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, distinctUntilChanged, map, of, switchMap } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Title } from '@angular/platform-browser';
import { DirectoryApi, LocationDetail } from './directory-api.service';

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

@Component({
  selector: 'ys-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="shell detail-shell">
      <a routerLink="/" class="backlink">← Към всички обекти</a>
      <div class="detail-loading" *ngIf="loading">Зареждане…</div>
      <div class="state-card error" *ngIf="error" role="alert">{{error}}</div>
      <div *ngIf="location as item">
        <div class="detail-hero">
          <div class="detail-icon">⌂</div>
          <div><span class="kicker">{{item.category}} · {{item.neighborhood}}</span><h1>{{item.name}}</h1>
            <p>{{item.description}}</p><span class="demo-note">Примерен обект · Информацията не е потвърдена</span>
          </div>
        </div>
        <div class="detail-grid">
          <article class="detail-main">
            <section class="panel">
              <h2>Актуални предложения</h2>
              <div class="empty-line" *ngIf="!item.offers.length">Няма активни предложения.</div>
              <div class="offer-line" *ngFor="let o of item.offers">
                <strong>✦ {{o.title}}</strong><p>{{o.description}}</p>
                <span>Офертата е валидна от <time [attr.datetime]="o.startsAtUtc">{{formatBulgarianDateTime(o.startsAtUtc)}}</time>
                  до <time [attr.datetime]="o.endsAtUtc">{{formatBulgarianDateTime(o.endsAtUtc)}}</time> (българско време)</span>
              </div>
            </section>
            <section class="panel">
              <h2>Брошури и каталози</h2>
              <div class="empty-line" *ngIf="!item.brochures.length">Няма активни брошури.</div>
              <div class="offer-line" *ngFor="let b of item.brochures">
                <strong>{{b.title}}</strong>
                <p>Валидност на брошурата:
                  <time [attr.datetime]="b.validFromUtc">{{formatBulgarianDateTime(b.validFromUtc)}}</time> –
                  <time [attr.datetime]="b.validToUtc">{{formatBulgarianDateTime(b.validToUtc)}}</time> (българско време)</p>
                <a [href]="b.url" rel="noopener noreferrer" target="_blank">Отвори брошурата ↗</a>
              </div>
            </section>
            <section class="panel">
              <h2>Работа в обекта</h2>
              <div class="empty-line" *ngIf="!item.jobs.length">В момента няма активни позиции.</div>
              <div class="offer-line" *ngFor="let j of item.jobs">
                <strong>{{j.title}}</strong><p>{{j.description}}</p>
                <span>Обявата е активна до <time [attr.datetime]="j.expiresAtUtc">{{formatBulgarianDateTime(j.expiresAtUtc)}}</time> (българско време)</span>
                <a [href]="j.applyUrl" rel="noopener noreferrer" target="_blank">Научи повече ↗</a>
              </div>
            </section>
          </article>
          <aside class="detail-sidebar panel">
            <h2>Информация</h2><div class="detail-info"><span>Локация</span><strong>{{item.neighborhood}}, {{item.city}}</strong></div>
            <div class="detail-info"><span>Адрес</span><strong>{{item.address}}</strong></div>
            <div class="detail-info"><span>Работно време</span><strong>{{item.openingHours || 'Не е посочено'}}</strong></div>
            <div class="detail-info" *ngIf="item.phone"><span>Телефон</span><a [href]="'tel:' + item.phone">{{item.phone}}</a></div>
            <div class="detail-info" *ngIf="item.website"><span>Уебсайт</span><a [href]="item.website" target="_blank" rel="noopener noreferrer">Посети сайта ↗</a></div>
            <small class="profile-updated">Профилът е редактиран на
              <time [attr.datetime]="item.updatedAtUtc">{{formatBulgarianDateTime(item.updatedAtUtc)}}</time> (българско време)</small>
          </aside>
        </div>
      </div>
    </section>
  `
})
export class DetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(DirectoryApi);
  private readonly title = inject(Title);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  location: LocationDetail | null = null;
  loading = true;
  error = '';

  formatBulgarianDateTime(value: string): string {
    const date = new Date(value);
    return Number.isFinite(date.getTime()) ? bulgarianDateTime.format(date) : 'Няма данни';
  }

  ngOnInit(): void {
    this.route.paramMap.pipe(
      map(params => params.get('slug')),
      distinctUntilChanged(),
      switchMap(slug => {
        this.location = null;
        this.loading = true;
        this.error = '';
        this.cdr.markForCheck();

        if (!slug) return of({ kind: 'missing' as const });

        return this.api.location(slug).pipe(
          map(location => ({ kind: 'loaded' as const, location })),
          catchError((reason: unknown) => of({
            kind: 'failed' as const,
            notFound: reason instanceof HttpErrorResponse && reason.status === 404
          }))
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(result => {
      this.loading = false;
      if (result.kind === 'loaded') {
        this.location = result.location;
        this.title.setTitle(result.location.name + ' | YangSpace');
      } else if (result.kind === 'missing') {
        this.error = 'Липсва идентификатор на обекта.';
      } else {
        this.error = result.notFound
          ? 'Обектът не е намерен или е скрит.'
          : 'Неуспешно зареждане. Провери връзката с API и опитай отново.';
      }
      this.cdr.markForCheck();
    });
  }
}

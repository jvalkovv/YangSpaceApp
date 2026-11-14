import { HttpErrorResponse } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { PublicAccountApi } from './public-account-api.service';

type AccountMode = 'login' | 'register' | 'recover' | 'reset' | 'resend' | 'account';

@Component({
  selector: 'ys-account',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section class="auth-shell shell" aria-labelledby="account-title">
      <div class="auth-intro">
        <span class="kicker">YANGSPACE · МОЯТ АКАУНТ</span>
        <h1>Твоето място в <span>квартала.</span></h1>
        <p>С един акаунт подготвяш присъствието на бизнеса си в YangSpace.
          Управлението на обект се активира само след отделно одобрение.</p>
        <div class="auth-points">
          <div><strong>01</strong><span>Създаваш личен профил</span></div>
          <div><strong>02</strong><span>Потвърждаваш имейла</span></div>
          <div><strong>03</strong><span>Заявяваш бизнес достъп след проверка</span></div>
        </div>
        <a routerLink="/" class="auth-back">← Обратно към обектите</a>
      </div>

      <div class="auth-card">
        <div class="auth-card-header">
          <span class="auth-icon" aria-hidden="true">Y.</span>
          <div>
            <span class="kicker">АКАУНТ</span>
            <h2 id="account-title">{{ title }}</h2>
          </div>
        </div>

        <p class="auth-description">{{ description }}</p>
        <p *ngIf="feedback" class="auth-message"
           [class.auth-error]="isError" role="status" aria-live="polite">{{ feedback }}</p>

        <ng-container *ngIf="mode === 'account'; else accountForm">
          <div *ngIf="session.profile() as profile; else signedOut" class="auth-profile">
            <p><strong>Имейл</strong><span>{{ profile.email }}</span></p>
            <p><strong>Бизнес права</strong><span>Само след отделно одобрение</span></p>
            <button type="button" class="btn btn-outline" (click)="logout()">Изход</button>
          </div>
          <ng-template #signedOut>
            <a routerLink="/login" class="btn btn-primary">Вход в акаунта</a>
          </ng-template>
        </ng-container>

        <ng-template #accountForm>
          <form *ngIf="!success" class="auth-form" (ngSubmit)="submit()">
            <label for="auth-email">Имейл адрес</label>
            <input id="auth-email" name="email" type="email" [(ngModel)]="email"
              autocomplete="email" maxlength="254" required placeholder="name@example.com" />

            <ng-container *ngIf="mode === 'login' || mode === 'register' || mode === 'reset'">
              <label for="auth-password">{{ mode === 'reset' ? 'Нова парола' : 'Парола' }}</label>
              <input id="auth-password" name="password" type="password"
                [(ngModel)]="password" minlength="12" maxlength="256" required
                [attr.autocomplete]="mode === 'login' ? 'current-password' : 'new-password'"
                placeholder="Поне 12 символа" />
            </ng-container>

            <ng-container *ngIf="mode === 'register'">
              <label for="auth-repeat">Повтори паролата</label>
              <input id="auth-repeat" name="repeatPassword" type="password"
                [(ngModel)]="repeatPassword" autocomplete="new-password"
                minlength="12" maxlength="256" required />
              <p class="auth-hint">Минимум 12 символа, главна буква, цифра и специален знак.
                Новият профил не получава автоматично права над бизнес обекти.</p>
            </ng-container>

            <ng-container *ngIf="mode === 'reset'">
              <label for="auth-code">Код от имейла</label>
              <textarea id="auth-code" name="resetCode" [(ngModel)]="resetCode"
                rows="3" maxlength="4096" required></textarea>
            </ng-container>

            <button type="submit" class="btn btn-primary auth-submit"
              [disabled]="busy">{{ busy ? 'Обработване…' : submitTitle }}</button>
          </form>
          <div *ngIf="success" class="auth-complete">
            <strong>{{ mode === 'login' ? 'Успешен вход' : 'Заявката е приета' }}</strong>
            <p>{{ successMessage }}</p>
            <a *ngIf="mode === 'login'" routerLink="/account" class="btn btn-primary">Моят профил</a>
            <a *ngIf="mode === 'reset'" routerLink="/login" class="btn btn-primary">Към вход</a>
          </div>

          <nav class="auth-links" aria-label="Акаунт действия">
            <a *ngIf="mode !== 'login'" routerLink="/login">Имам акаунт</a>
            <a *ngIf="mode !== 'register'" routerLink="/register">Нов акаунт</a>
            <a *ngIf="mode !== 'recover'" routerLink="/recover">Забравена парола</a>
            <a *ngIf="mode !== 'resend'" routerLink="/resend-confirmation">Повторно потвърждение</a>
          </nav>
        </ng-template>
      </div>
    </section>
  `
})
export class AccountComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyed = inject(DestroyRef);
  readonly session = inject(PublicAccountApi);

  mode: AccountMode = 'login';
  email = '';
  password = '';
  repeatPassword = '';
  resetCode = '';
  busy = false;
  success = false;
  isError = false;
  feedback = '';

  get title(): string {
    const titles: Record<AccountMode, string> = {
      login: 'Вход', register: 'Регистрация', recover: 'Забравена парола',
      reset: 'Нова парола', resend: 'Потвърждение на имейл', account: 'Моят профил'
    };
    return titles[this.mode];
  }
  get description(): string {
    const texts: Record<AccountMode, string> = {
      login: 'Влез с потвърдения си имейл адрес.',
      register: 'Създай акаунт. Ще ти изпратим имейл за потвърждение.',
      recover: 'Ще изпратим код, ако имейлът е свързан с потвърден акаунт.',
      reset: 'Въведи получения код и избери нова парола.',
      resend: 'При непотвърден акаунт ще изпратим нов линк.',
      account: 'Тук ще виждаш достъпа си до YangSpace.'
    };
    return texts[this.mode];
  }
  get submitTitle(): string {
    const names: Record<AccountMode, string> = {
      login: 'Вход', register: 'Създай акаунт', recover: 'Изпрати код',
      reset: 'Запази новата парола', resend: 'Изпрати потвърждение', account: ''
    };
    return names[this.mode];
  }
  get successMessage(): string {
    const names: Record<AccountMode, string> = {
      login: 'Вече си в акаунта си.',
      register: 'Ако регистрацията е нова, провери входящата си поща и потвърди имейла.',
      recover: 'Ако адресът съществува и е потвърден, ще получиш код за възстановяване.',
      reset: 'Ако кодът е валиден, паролата е променена. Влез отново.',
      resend: 'Ако акаунтът очаква потвърждение, ще получиш нов линк.',
      account: ''
    };
    return names[this.mode];
  }

  ngOnInit(): void {
    this.route.data.pipe(takeUntilDestroyed(this.destroyed)).subscribe(data => {
      this.mode = data['mode'] as AccountMode || 'login';
      this.busy = false; this.success = false; this.feedback = ''; this.isError = false;
      this.password = ''; this.repeatPassword = ''; this.resetCode = '';
    });
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyed)).subscribe(query => {
      if (this.mode === 'login' && query.get('confirmed') !== null) {
        this.isError = query.get('confirmed') !== '1';
        this.feedback = this.isError
          ? 'Линкът е невалиден или е изтекъл. Поискай ново потвърждение.'
          : 'Имейлът е потвърден. Вече можеш да влезеш.';
      }
    });
  }

  submit(): void {
    if (this.busy || this.mode === 'account') return;
    this.feedback = ''; this.isError = false;
    const email = this.email.trim();
    if (!email || !email.includes('@')) { this.showError('Въведи валиден имейл.'); return; }
    if (['login','register','reset'].includes(this.mode) && !this.password) {
      this.showError('Въведи парола.'); return;
    }
    if (this.mode === 'register' && this.password !== this.repeatPassword) {
      this.showError('Паролите не съвпадат.'); return;
    }
    if (this.mode === 'reset' && !this.resetCode.trim()) {
      this.showError('Въведи кода от имейла.'); return;
    }

    this.busy = true;
    let action: Observable<unknown>;
    switch (this.mode) {
      case 'login': action = this.session.login(email, this.password); break;
      case 'register': action = this.session.register(email, this.password); break;
      case 'recover': action = this.session.requestReset(email); break;
      case 'reset': action = this.session.resetPassword(email, this.resetCode.trim(), this.password); break;
      case 'resend': action = this.session.resendConfirmation(email); break;
      default: this.busy = false; return;
    }
    action.pipe(takeUntilDestroyed(this.destroyed)).subscribe({
      next: () => {
        this.busy = false; this.success = true;
        this.password = ''; this.repeatPassword = ''; this.resetCode = '';
      },
      error: (error: unknown) => {
        this.busy = false;
        if (error instanceof HttpErrorResponse) {
          if (error.status === 404 || error.status === 503)
            this.showError('Регистрацията и входът още не са активни. Опитай по-късно.');
          else if (error.status === 429)
            this.showError('Твърде много опити. Опитай отново след малко.');
          else if (error.status === 401)
            this.showError('Неуспешен вход. Провери данните и потвърждението на имейла.');
          else if (error.status === 400)
            this.showError('Невалидни данни. Провери паролата или кода от имейла.');
          else this.showError('Възникна временен проблем с услугата.');
        } else this.showError('Сесията не може да бъде потвърдена. Опитай отново.');
      }
    });
  }

  logout(): void {
    this.session.logout();
    void this.router.navigateByUrl('/login');
  }

  private showError(message: string): void {
    this.isError = true;
    this.feedback = message;
  }
}

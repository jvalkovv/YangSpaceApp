import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'ys-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  template: `
    <a class="skip-link" href="#main-content">Към основното съдържание</a>
    <header class="topbar">
      <div class="shell topbar-inner">
        <a routerLink="/" class="wordmark" aria-label="YangSpace — начална страница" (click)="closeMenu()">
          <span class="brand-mark" aria-hidden="true">Y.</span>
          <span>Yang<span class="brand-accent">Space</span></span>
        </a>
        <nav class="mainnav" [class.open]="menuOpen" aria-label="Основна навигация" id="primary-nav">
          <a routerLink="/" fragment="catalog" (click)="closeMenu()">Обекти</a>
          <a routerLink="/" fragment="offers" (click)="closeMenu()">Промоции</a>
          <a routerLink="/" fragment="jobs" (click)="closeMenu()">Работа</a>
          <a routerLink="/" fragment="business" (click)="closeMenu()">За бизнеса</a>
        </nav>
        <span class="preview-pill" title="Демонстрационни данни">ДЕМО ВЕРСИЯ</span>
        <button class="menu-toggle" type="button" [attr.aria-expanded]="menuOpen"
                aria-controls="primary-nav" aria-label="Отвори или затвори навигацията"
                (click)="menuOpen = !menuOpen">
          <span></span><span></span><span></span>
        </button>
      </div>
    </header>
    <main id="main-content"><router-outlet /></main>
    <footer class="site-footer">
      <div class="shell footer-top">
        <div><a routerLink="/" class="footer-logo">Yang<span>Space.</span></a>
          <p>По-близо до местата, хората и възможностите наоколо.</p>
        </div>
        <div class="footer-nav">
          <a routerLink="/" fragment="catalog">Обекти</a>
          <a routerLink="/" fragment="offers">Оферти</a>
          <a routerLink="/" fragment="jobs">Работа</a>
        </div>
      </div>
      <div class="shell footer-bottom">
        <span>© YangSpace · Пилотна версия за Варна</span>
        <span>Демо обекти и предложения · Не са реални</span>
      </div>
    </footer>
  `
})
export class AppComponent {
  menuOpen = false;
  closeMenu(): void { this.menuOpen = false; }
}

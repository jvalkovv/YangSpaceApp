import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'ys-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="topbar">
      <div class="shell topbar-inner">
        <a routerLink="/" class="wordmark" aria-label="YangSpace — начална страница">
          <span class="brand-mark">Y.</span><span>Yang<span class="brand-accent">Space</span></span>
        </a>
        <nav class="mainnav" aria-label="Основна навигация">
          <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Открий квартала</a>
          <a routerLink="/admin" routerLinkActive="active">За търговци</a>
        </nav>
        <span class="preview-pill">DEMO PREVIEW</span>
      </div>
    </header>
    <main><router-outlet /></main>
    <footer class="site-footer">
      <div class="shell footer-inner"><span>© YangSpace · Изследваме квартала заедно.</span><span>Демонстрационни обекти — данните не са реални.</span></div>
    </footer>
  `
})
export class AppComponent {}

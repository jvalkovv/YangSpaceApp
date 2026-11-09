import { expect, test } from '@playwright/test';

// Deterministic, fictional preview fixtures. Never require a running .NET API.
const previewLocations = [
  ['bike-demo', 'Вело сервиз „Педал“', 'Услуги', 'Левски'],
  ['pet-demo', 'Зоомагазин „Мустаци“', 'Магазини', 'Чайка'],
  ['cafe-demo', 'Кафене „Ъгълът“', 'Заведения', 'Левски'],
  ['market-demo', 'Магазин „При нас“', 'Хранителни магазини', 'Цветен квартал'],
  ['bakery-demo', 'Пекарна „Сутрин“', 'Пекарни', 'Цветен квартал'],
  ['studio-demo', 'Студио „Линия“', 'Красота', 'Цветен квартал']
].map(([slug, name, category, neighborhood], index) => ({
  id: `00000000-0000-4000-8000-${String(index + 1).padStart(12, '0')}`,
  slug, name, category, neighborhood,
  description: 'Примерно съдържание на обекта.',
  address: 'Демо адрес',
  imageUrl: null,
  isVerified: false,
  activeOffers: 1,
  activeJobs: 0
}));

const homepage = {
  meta: {
    cities: ['Варна'],
    neighborhoods: ['Левски', 'Цветен квартал', 'Чайка'],
    categories: [...new Set(previewLocations.map(location => location.category))]
  },
  directory: { items: previewLocations, page: 1, pageSize: 12, total: 6 },
  offers: [], brochures: [], jobs: []
};

for (const width of [375, 390, 768, 1024, 1440]) {
  test(`homepage fits ${width}px viewport, renders cards, and captures layout`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width, height: 900 });
    await page.route('**/api/v1/homepage', route => route.fulfill({ json: homepage }));
    await page.goto('/');

    await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(6);
    await expect(page.locator('.hero h1')).toBeVisible();
    await expect(page.locator('.search-panel')).toBeVisible();
    await expect(page.getByRole('link', { name: /Вело сервиз „Педал“/ })).toBeVisible();

    const layout = await page.evaluate(() => ({
      viewport: window.innerWidth,
      document: document.documentElement.scrollWidth,
      body: document.body.scrollWidth
    }));
    expect(layout.viewport).toBe(width);
    expect(layout.document, 'document must not overflow horizontally').toBeLessThanOrEqual(width + 1);
    expect(layout.body, 'body must not overflow horizontally').toBeLessThanOrEqual(width + 1);

    const mobile = width <= 760;
    if (mobile) {
      await expect(page.locator('.menu-toggle')).toBeVisible();
      await expect(page.locator('.hero-visual')).toBeHidden();
    } else {
      await expect(page.locator('.menu-toggle')).toBeHidden();
      await expect(page.locator('.hero-visual')).toBeVisible();
      if (width === 768) {
        // Page-level overflow assertions do not detect children cropped by
        // .hero { overflow:hidden }; assert the actual map's visible bounds.
        const bounds = await page.evaluate(() => {
          const map = document.querySelector('.neighborhood-map')!.getBoundingClientRect();
          const feature = document.querySelector('.map-feature')!.getBoundingClientRect();
          const hero = document.querySelector('.hero')!.getBoundingClientRect();
          return {
            mapLeft: map.left, mapRight: map.right, heroLeft: hero.left, heroRight: hero.right,
            featureLeft: feature.left, featureRight: feature.right,
            featureTop: feature.top, featureBottom: feature.bottom,
            mapTop: map.top, mapBottom: map.bottom
          };
        });
        expect(bounds.mapLeft).toBeGreaterThanOrEqual(bounds.heroLeft - 1);
        expect(bounds.mapRight).toBeLessThanOrEqual(bounds.heroRight + 1);
        expect(bounds.featureLeft).toBeGreaterThanOrEqual(bounds.mapLeft - 1);
        expect(bounds.featureRight).toBeLessThanOrEqual(bounds.mapRight + 1);
        expect(bounds.featureTop).toBeGreaterThanOrEqual(bounds.mapTop - 1);
        expect(bounds.featureBottom).toBeLessThanOrEqual(bounds.mapBottom + 1);
      }
    }

    // Evidence is retained under test-results, ignored by Git.
    await page.screenshot({
      path: testInfo.outputPath(`yangspace-home-${width}.png`),
      fullPage: true,
      animations: 'disabled'
    });
  });
}

const populatedHomepage = {
  ...homepage,
  offers: [
    { slug: 'bike-demo', locationName: 'Вело сервиз „Педал“', neighborhood: 'Левски', category: 'Услуги', title: 'Седмична оферта за сервиз', expiresAtUtc: '2026-12-01T12:00:00Z' },
    { slug: 'pet-demo', locationName: 'Зоомагазин „Мустаци“', neighborhood: 'Чайка', category: 'Магазини', title: 'Примерно предложение за аксесоари', expiresAtUtc: '2026-12-02T12:00:00Z' },
    { slug: 'studio-demo', locationName: 'Студио „Линия“', neighborhood: 'Цветен квартал', category: 'Красота', title: 'Демо пакет за грижа', expiresAtUtc: '2026-12-03T12:00:00Z' }
  ],
  brochures: [
    { slug: 'market-demo', locationName: 'Магазин „При нас“', neighborhood: 'Цветен квартал', category: 'Хранителни магазини', title: 'Седмична демо брошура', expiresAtUtc: '2026-12-01T12:00:00Z' }
  ],
  jobs: [
    { slug: 'cafe-demo', locationName: 'Кафене „Ъгълът“', neighborhood: 'Левски', category: 'Заведения', title: 'Бариста — примерна позиция', expiresAtUtc: '2026-12-01T12:00:00Z' }
  ]
};

for (const width of [390, 768, 1440]) {
  test(`populated offers, brochures and jobs fit ${width}px viewport`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width, height: 900 });
    await page.route('**/api/v1/homepage', route => route.fulfill({ json: populatedHomepage }));
    await page.goto('/');
    await expect(page.locator('.discover-offers .discover-item')).toHaveCount(3);
    await expect(page.locator('.discover-brochures .discover-item')).toHaveCount(1);
    await expect(page.locator('.discover-jobs .discover-item')).toHaveCount(1);
    const metrics = await page.evaluate(() => ({
      rootWidth: document.documentElement.scrollWidth,
      bodyWidth: document.body.scrollWidth,
      viewport: window.innerWidth
    }));
    expect(metrics.rootWidth).toBeLessThanOrEqual(metrics.viewport + 1);
    expect(metrics.bodyWidth).toBeLessThanOrEqual(metrics.viewport + 1);
    await page.screenshot({
      path: testInfo.outputPath(`yangspace-populated-${width}.png`),
      fullPage: true,
      animations: 'disabled'
    });
  });
}

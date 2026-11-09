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
    }

    // Evidence is retained under test-results, ignored by Git.
    await page.screenshot({
      path: testInfo.outputPath(`yangspace-home-${width}.png`),
      fullPage: true,
      animations: 'disabled'
    });
  });
}

import { expect, test } from '@playwright/test';

const bike = {
  id: '11111111-1111-4111-8111-111111111111',
  slug: 'bike-demo',
  name: 'Вело сервиз „Педал“',
  neighborhood: 'Левски',
  category: 'Услуги',
  description: 'Сервиз и поддръжка на велосипеди.',
  address: 'Демо обект',
  imageUrl: null,
  isVerified: false,
  activeOffers: 1,
  activeJobs: 0
};
const studio = {
  ...bike, id: '22222222-2222-4222-8222-222222222222', slug: 'studio-demo',
  name: 'Студио „Линия“', neighborhood: 'Цветен квартал',
  category: 'Красота', description: 'Подстригване и грижа.'
};
const cafe = {
  ...bike, id: '33333333-3333-4333-8333-333333333333', slug: 'cafe-demo',
  name: 'Кафене „Ъгълът“', category: 'Заведения',
  description: 'Кафе и десерти.'
};
const seed = [bike, studio, cafe];

const homepage = {
  meta: {
    cities: ['Варна'],
    neighborhoods: ['Левски', 'Цветен квартал'],
    categories: ['Красота', 'Заведения', 'Услуги']
  },
  directory: { page: 1, pageSize: 12, total: seed.length, items: seed },
  offers: [], brochures: [], jobs: []
};

test('initial API data renders without focus, click, or blur', async ({ page }) => {
  await page.route('**/api/v1/homepage', async route => {
    await route.fulfill({ json: homepage });
  });
  await page.goto('/');
  await expect(page.locator('.location-card')).toHaveCount(3);
  await expect(page.getByRole('link', { name: /Вело сервиз „Педал“/ })).toBeVisible();
  await expect(page.locator('.skeleton-grid')).toHaveCount(0);
  await expect(page.locator('.results-count')).toHaveText('3 обекта');
});

test('category change immediately clears old cards, then shows only matching results', async ({ page }) => {
  await page.route('**/api/v1/homepage', route => route.fulfill({ json: homepage }));
  await page.route('**/api/v1/locations?*', async route => {
    const category = new URL(route.request().url()).searchParams.get('category');
    await new Promise(resolve => setTimeout(resolve, 400));
    const matches = category ? seed.filter(x => x.category === category) : seed;
    await route.fulfill({
      json: { items: matches, page: 1, pageSize: 12, total: matches.length }
    });
  });
  await page.goto('/');
  await expect(page.locator('.location-card')).toHaveCount(3);
  await page.getByRole('button', { name: /Красота/ }).click();
  // Stale cafe cards must not be visible while the API is in flight.
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(0);
  await expect(page.locator('.skeleton-card')).toHaveCount(6);
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(1);
  await expect(page.locator('.location-card').filter({ hasText: 'Студио „Линия“' })).toBeVisible();
  await expect(page.locator('.location-card').filter({ hasText: 'Кафене „Ъгълът“' })).toHaveCount(0);
});

test('business detail resolves from a fresh navigation without another interaction', async ({ page }) => {
  await page.route('**/api/v1/locations/bike-demo', route => route.fulfill({
    json: {
      ...bike,
      city: 'Варна',
      openingHours: 'Демонстрационно',
      phone: null,
      website: null,
      updatedAtUtc: '2026-11-08T12:00:00Z',
      offers: [{
        id: '44444444-4444-4444-8444-444444444444',
        title: 'Примерна оферта',
        description: 'Демо съдържание.',
        startsAtUtc: '2026-11-07T12:00:00Z',
        endsAtUtc: '2026-12-08T12:00:00Z'
      }],
      brochures: [],
      jobs: []
    }
  }));
  await page.goto('/obekt/bike-demo');
  await expect(page.getByRole('heading', { name: 'Вело сервиз „Педал“' })).toBeVisible();
  await expect(page.locator('.detail-loading')).toHaveCount(0);
  await expect(page.getByText('Примерна оферта')).toBeVisible();
});

test('HTTP errors render promptly without requiring user input', async ({ page }) => {
  await page.route('**/api/v1/homepage', route => route.fulfill({ status: 503, body: 'Unavailable' }));
  await page.goto('/');
  await expect(page.getByRole('alert')).toContainText('Не успяхме да заредим обектите.');
});

test('preview admin async login/list renders without another click', async ({ page }) => {
  await page.route('**/api/auth/login', route => route.fulfill({
    json: { accessToken: 'mock-preview-token', tokenType: 'Bearer', expiresIn: 3600, refreshToken: 'unused' }
  }));
  await page.route('**/api/v1/admin/locations', route => route.fulfill({ json: [{
    ...bike, isPublished: true, openingHours: 'Демо', description: bike.description
  }] }));
  await page.goto('/admin');
  await page.getByLabel('Имейл').fill('preview@example.invalid');
  await page.getByLabel('Парола').fill('TestPassword123!');
  await page.getByRole('button', { name: 'Вход' }).click();
  await expect(page.getByRole('heading', { name: 'Обекти (1)' })).toBeVisible();
});

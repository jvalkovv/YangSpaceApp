import { expect, test } from '@playwright/test';

const items = [
  {
    id: '11111111-1111-4111-8111-111111111111',
    slug: 'bike-demo', name: 'Вело сервиз „Педал“',
    category: 'Услуги', neighborhood: 'Левски',
    description: 'Сервиз за велосипеди.', address: 'Демо адрес',
    imageUrl: null, isVerified: false, activeOffers: 1, activeJobs: 0
  },
  {
    id: '22222222-2222-4222-8222-222222222222',
    slug: 'cafe-demo', name: 'Кафене „Ъгълът“',
    category: 'Заведения', neighborhood: 'Чайка',
    description: 'Кафе и сладкиши.', address: 'Демо адрес',
    imageUrl: null, isVerified: false, activeOffers: 0, activeJobs: 0
  }
];
const homepage = {
  meta: { cities: ['Варна'], neighborhoods: ['Левски', 'Чайка'], categories: ['Услуги', 'Заведения'] },
  directory: { page: 1, pageSize: 12, total: items.length, items },
  offers: [{
    slug: 'bike-demo', locationName: 'Вело сервиз „Педал“', neighborhood: 'Левски',
    category: 'Услуги', title: 'Активна оферта', expiresAtUtc: '2026-12-01T18:00:00Z'
  }],
  brochures: [], jobs: []
};

test('anonymous visitor can filter the directory, open a profile and return to the catalogue', async ({ page }) => {
  await page.route('**/api/v1/homepage', route => route.fulfill({ json: homepage }));
  await page.route('**/api/v1/locations?*', route => {
    const category = new URL(route.request().url()).searchParams.get('category') ?? '';
    const results = category ? items.filter(item => item.category === category) : items;
    return route.fulfill({ json: { items: results, page: 1, pageSize: 12, total: results.length } });
  });
  await page.route('**/api/v1/locations/bike-demo', route => route.fulfill({
    json: {
      ...items[0], city: 'Варна', phone: null, website: null, openingHours: null,
      updatedAtUtc: '2026-11-08T12:00:00Z',
      offers: [{
        id: 'offer-bike-demo', title: 'Активна оферта', description: 'Демо предложение.',
        startsAtUtc: '2026-11-01T08:00:00Z', endsAtUtc: '2026-12-01T18:00:00Z'
      }],
      brochures: [], jobs: []
    }
  }));

  await page.goto('/');
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(2);
  await page.getByRole('button', { name: /Услуги/ }).click();
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(1);
  const bikeCard = page.locator('.location-card').filter({ hasText: 'Вело сервиз „Педал“' });
  await expect(bikeCard).toBeVisible();
  await expect(page.locator('.location-card').filter({ hasText: 'Кафене „Ъгълът“' })).toHaveCount(0);

  await bikeCard.click();
  await expect(page).toHaveURL(/\/obekt\/bike-demo$/);
  await expect(page.getByRole('heading', { name: 'Вело сервиз „Педал“' })).toBeVisible();
  await expect(page.locator('.detail-sidebar')).toBeVisible();
  await expect(page.locator('.detail-entry').filter({ hasText: 'Активна оферта' })).toBeVisible();
  await page.getByRole('link', { name: /Към всички обекти/ }).click();
  await expect(page).toHaveURL(/\/$/);
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(2);
  await expect(page.locator('.detail-hero')).toHaveCount(0);
});

test('public search remains anonymous and uses the directory endpoint', async ({ page }) => {
  const searches: string[] = [];
  await page.route('**/api/v1/homepage', route => route.fulfill({ json: homepage }));
  await page.route('**/api/v1/locations?*', route => {
    const q = new URL(route.request().url()).searchParams.get('q') ?? '';
    searches.push(q);
    const matches = q ? items.filter(item => item.name.toLocaleLowerCase('bg-BG').includes(q.toLocaleLowerCase('bg-BG'))) : items;
    return route.fulfill({ json: { items: matches, page: 1, pageSize: 12, total: matches.length } });
  });
  await page.goto('/');
  await page.getByRole('searchbox', { name: 'Име на обект или ключова дума' }).fill('Педал');
  await page.getByRole('button', { name: /Търси/ }).click();
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(1);
  await expect(page.locator('.location-card').filter({ hasText: 'Вело сервиз „Педал“' })).toBeVisible();
  expect(searches).toContain('Педал');
  await expect(page.locator('.account-nav a[href="/register"]')).toBeVisible();
});

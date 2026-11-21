import { expect, test } from '@playwright/test';

const locations = [
  {
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
  },
  {
    id: '22222222-2222-4222-8222-222222222222',
    slug: 'cafe-demo',
    name: 'Кафене „Ъгълът“',
    neighborhood: 'Левски',
    category: 'Заведения',
    description: 'Кафе и десерти.',
    address: 'Демо обект',
    imageUrl: null,
    isVerified: false,
    activeOffers: 0,
    activeJobs: 1
  }
];

const homepage = {
  meta: {
    cities: ['Варна'],
    neighborhoods: ['Левски'],
    categories: ['Заведения', 'Услуги']
  },
  directory: { page: 1, pageSize: 12, total: locations.length, items: locations },
  offers: [{
    slug: 'bike-demo', locationName: 'Вело сервиз „Педал“', neighborhood: 'Левски',
    category: 'Услуги', title: 'Пролетна оферта', expiresAtUtc: '2026-12-31T23:59:00Z'
  }],
  brochures: [{
    slug: 'bike-demo', locationName: 'Вело сервиз „Педал“', neighborhood: 'Левски',
    category: 'Услуги', title: 'Зимен каталог', expiresAtUtc: '2026-12-31T23:59:00Z'
  }],
  jobs: [{
    slug: 'cafe-demo', locationName: 'Кафене „Ъгълът“', neighborhood: 'Левски',
    category: 'Заведения', title: 'Бариста — свободна позиция', expiresAtUtc: '2026-12-31T23:59:00Z'
  }]
};

const highlights = [
  {
    selector: '.discover-offers', unavailable: 'Не успяхме да заредим офертите.',
    empty: 'Няма активни предложения в момента.', retry: 'Повтори зареждането на офертите'
  },
  {
    selector: '.discover-brochures', unavailable: 'Не успяхме да заредим брошурите.',
    empty: 'Няма активни брошури в момента.', retry: 'Повтори зареждането на брошурите'
  },
  {
    selector: '.discover-jobs', unavailable: 'Не успяхме да заредим позициите.',
    empty: 'Няма активни позиции в момента.', retry: 'Повтори зареждането на позициите'
  }
];

test('homepage highlights show loading while the bootstrap request is pending', async ({ page }) => {
  await page.route('**/api/v1/homepage', async route => {
    await new Promise(resolve => setTimeout(resolve, 150));
    await route.fulfill({ json: homepage });
  });
  await page.goto('/');

  for (const highlight of highlights) {
    await expect(page.locator(highlight.selector).getByRole('status')).toContainText('Зареждаме');
  }
  await expect(page.locator('.discover-offers .discover-item')).toContainText('Пролетна оферта');
  await expect(page.locator('.discover-brochures .discover-item')).toContainText('Зимен каталог');
  await expect(page.locator('.discover-jobs .discover-item')).toContainText('Бариста — свободна позиция');
});

test('HTTP 503 marks all homepage highlights unavailable instead of empty', async ({ page }, testInfo) => {
  await page.route('**/api/v1/homepage', route => route.fulfill({
    status: 503,
    body: 'Service Unavailable'
  }));
  await page.goto('/');

  for (const highlight of highlights) {
    const card = page.locator(highlight.selector);
    await expect(card.getByRole('status')).toContainText(highlight.unavailable);
    await expect(card).not.toContainText(highlight.empty);
    await expect(card.getByRole('button', { name: highlight.retry })).toBeVisible();
  }
  await page.locator('.offers-section').scrollIntoViewIfNeeded();
  await page.screenshot({ path: testInfo.outputPath('homepage-highlights-unavailable.png') });
});

test('highlight retry reloads bootstrap and renders populated offers, brochures and jobs', async ({ page }, testInfo) => {
  const unexpectedConsoleErrors: string[] = [];
  const uncaughtBrowserErrors: string[] = [];
  page.on('console', message => {
    if (message.type() === 'error' && !message.text().includes('503 (Service Unavailable)')) {
      unexpectedConsoleErrors.push(message.text());
    }
  });
  page.on('pageerror', error => uncaughtBrowserErrors.push(error.message));
  let bootstrapAttempts = 0;
  await page.route('**/api/v1/homepage', route => {
    bootstrapAttempts += 1;
    return bootstrapAttempts === 1
      ? route.fulfill({ status: 503, body: 'Service Unavailable' })
      : route.fulfill({ json: homepage });
  });
  await page.goto('/');
  await expect(page.locator('.discover-offers').getByRole('status')).toContainText('Не успяхме да заредим офертите.');

  await page.locator('.discover-offers').getByRole('button', { name: 'Повтори зареждането на офертите' }).click();

  await expect(page.locator('.discover-offers .discover-item')).toContainText('Пролетна оферта');
  await expect(page.locator('.discover-brochures .discover-item')).toContainText('Зимен каталог');
  await expect(page.locator('.discover-jobs .discover-item')).toContainText('Бариста — свободна позиция');
  for (const highlight of highlights) {
    await expect(page.locator(highlight.selector).locator('.discover-unavailable')).toHaveCount(0);
    await expect(page.locator(highlight.selector)).not.toContainText(highlight.empty);
  }
  await page.locator('.offers-section').scrollIntoViewIfNeeded();
  await page.screenshot({ path: testInfo.outputPath('homepage-highlights-recovered.png') });
  expect(unexpectedConsoleErrors).toEqual([]);
  expect(uncaughtBrowserErrors).toEqual([]);
  expect(bootstrapAttempts).toBe(2);
});

test('a successful empty homepage response shows genuine empty states for each highlight', async ({ page }) => {
  await page.route('**/api/v1/homepage', route => route.fulfill({
    json: {
      ...homepage,
      directory: {
        ...homepage.directory,
        items: locations.map(location => ({ ...location, activeOffers: 0, activeJobs: 0 }))
      },
      offers: [], brochures: [], jobs: []
    }
  }));
  await page.goto('/');

  for (const highlight of highlights) {
    const card = page.locator(highlight.selector);
    await expect(card.getByRole('status')).toContainText(highlight.empty);
    await expect(card.locator('.discover-unavailable')).toHaveCount(0);
    await expect(card.getByRole('button')).toHaveCount(0);
  }
});

test('failed bootstrap never renders stale highlight payload data', async ({ page }) => {
  await page.route('**/api/v1/homepage', route => route.fulfill({
    status: 503,
    contentType: 'application/json',
    body: JSON.stringify({
      ...homepage,
      offers: [{ ...homepage.offers[0], title: 'Остаряла оферта' }],
      brochures: [{ ...homepage.brochures[0], title: 'Остаряла брошура' }],
      jobs: [{ ...homepage.jobs[0], title: 'Остаряла обява' }]
    })
  }));
  await page.goto('/');

  for (const highlight of highlights) {
    const card = page.locator(highlight.selector);
    await expect(card.getByRole('status')).toContainText(highlight.unavailable);
  }
  await expect(page.getByText('Остаряла оферта')).toHaveCount(0);
  await expect(page.getByText('Остаряла брошура')).toHaveCount(0);
  await expect(page.getByText('Остаряла обява')).toHaveCount(0);
});

test('highlight retry preserves an active directory search and its results', async ({ page }) => {
  let bootstrapAttempts = 0;
  await page.route('**/api/v1/homepage', route => {
    bootstrapAttempts += 1;
    return bootstrapAttempts === 1
      ? route.fulfill({ status: 503, body: 'Service Unavailable' })
      : route.fulfill({ json: homepage });
  });
  await page.route('**/api/v1/locations?*', route => {
    const query = new URL(route.request().url()).searchParams.get('q') ?? '';
    const items = query === 'Кафене' ? [locations[1]] : locations;
    return route.fulfill({
      json: { page: 1, pageSize: 12, total: items.length, items }
    });
  });
  await page.goto('/');
  await page.locator('input[name="q"]').fill('Кафене');
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(1);
  await expect(page.locator('.location-card').filter({ hasText: 'Кафене „Ъгълът“' })).toBeVisible();

  await page.locator('.discover-offers').getByRole('button', { name: 'Повтори зареждането на офертите' }).click();

  await expect(page.locator('.discover-offers .discover-item')).toContainText('Пролетна оферта');
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(1);
  await expect(page.locator('.location-card').filter({ hasText: 'Кафене „Ъгълът“' })).toBeVisible();
  await expect(page.locator('.location-card').filter({ hasText: 'Вело сервиз „Педал“' })).toHaveCount(0);
  expect(bootstrapAttempts).toBe(2);
});

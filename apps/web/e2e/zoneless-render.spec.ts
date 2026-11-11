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
  await page.route('**/api/v1/admin/me', route => route.fulfill({ json: {
    email: 'preview@example.invalid', role: 'PlatformAdmin'
  } }));
  await page.route('**/api/v1/admin/locations', route => route.fulfill({ json: [{
    ...bike, isPublished: true, openingHours: 'Демо', description: bike.description
  }] }));
  await page.goto('/admin');
  await page.getByLabel('Имейл').fill('preview@example.invalid');
  await page.getByLabel('Парола').fill('TestPassword123!');
  await page.getByRole('button', { name: 'Вход' }).click();
  await expect(page.getByRole('heading', { name: 'Обекти (1)' })).toBeVisible();
});

test('first category click and dropdown change send the selected value, never stale content', async ({ page }) => {
  const shop = {
    ...bike,
    id: '55555555-5555-4555-8555-555555555555',
    slug: 'market-demo',
    name: 'Магазин „При нас“',
    category: 'Магазини',
    neighborhood: 'Цветен квартал'
  };
  const items = [...seed, shop];
  const seenCategories: string[] = [];
  await page.route('**/api/v1/homepage', route => route.fulfill({
    json: {
      ...homepage,
      meta: { ...homepage.meta, categories: [...homepage.meta.categories, 'Магазини'] },
      directory: { ...homepage.directory, total: items.length, items }
    }
  }));
  await page.route('**/api/v1/locations?*', async route => {
    const category = new URL(route.request().url()).searchParams.get('category') ?? '';
    seenCategories.push(category);
    await new Promise(resolve => setTimeout(resolve, 320));
    try {
      const result = category ? items.filter(item => item.category === category) : items;
      await route.fulfill({ json: { page: 1, pageSize: 12, total: result.length, items: result } });
    } catch {
      // An obsolete request can be aborted by Angular when selection changes.
    }
  });

  await page.goto('/');
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(4);

  // Click a category chip: the corresponding dropdown must follow the same state.
  await page.getByRole('button', { name: /Магазини/ }).click();
  await expect(page.getByRole('combobox', { name: 'Избери категория' })).toHaveValue('Магазини');
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(0);
  await expect(page.locator('.skeleton-card')).toHaveCount(6);
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(1);
  await expect(page.locator('.location-card').filter({ hasText: 'Магазин „При нас“' })).toBeVisible();
  await expect(page.locator('.location-card').filter({ hasText: 'Студио „Линия“' })).toHaveCount(0);
  expect(seenCategories).toContain('Магазини');

  // Change the dropdown directly: never read a stale two-way ngModel field.
  await page.getByRole('combobox', { name: 'Избери категория' }).selectOption('Красота');
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(0);
  await expect(page.locator('.location-card').filter({ hasText: 'Студио „Линия“' })).toBeVisible();
  await expect(page.locator('.location-card').filter({ hasText: 'Магазин „При нас“' })).toHaveCount(0);
  expect(seenCategories.at(-1)).toBe('Красота');
});

test('brand badge is one centered text item and desktop/mobile layouts do not overflow', async ({ page }) => {
  await page.route('**/api/v1/homepage', route => route.fulfill({ json: homepage }));
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto('/');
  const brand = page.locator('.brand-mark');
  await expect(brand).toHaveText('Y.');
  await expect(brand.locator('span')).toHaveCount(0);
  const desktopOverflow = await page.evaluate(() =>
    document.documentElement.scrollWidth > document.documentElement.clientWidth
  );
  expect(desktopOverflow).toBe(false);
  await page.setViewportSize({ width: 390, height: 844 });
  const mobileOverflow = await page.evaluate(() =>
    document.documentElement.scrollWidth > document.documentElement.clientWidth
  );
  expect(mobileOverflow).toBe(false);
});

test('a failed initial API load does not pretend that zero locations exist; retry restores all sections', async ({ page }) => {
  let online = false;
  await page.route('**/api/v1/homepage', async route => {
    if (!online) {
      await route.fulfill({ status: 503, body: 'Service Unavailable' });
      return;
    }
    await route.fulfill({ json: homepage });
  });
  await page.goto('/');
  await expect(page.getByRole('alert')).toContainText('Услугата е временно недостъпна.');
  await expect(page.getByRole('alert')).not.toContainText(/127\\.0\\.0\\.1|localhost|\\.NET|Angular|proxy/i);
  await expect(page.locator('.results-count')).toHaveCount(0);
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(0);
  online = true;
  await page.getByRole('button', { name: 'Опитай отново' }).click();
  await expect(page.getByRole('alert')).toHaveCount(0);
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(3);
  await expect(page.locator('.results-count')).toHaveText('3 обекта');
});

test('filtered API 502 shows a service error, not an authoritative empty result, then recovers', async ({ page }) => {
  let online = false;
  await page.route('**/api/v1/homepage', route => route.fulfill({ json: homepage }));
  await page.route('**/api/v1/locations?*', async route => {
    if (!online) {
      await route.fulfill({ status: 502, body: 'Bad Gateway' });
      return;
    }
    const category = new URL(route.request().url()).searchParams.get('category');
    const items = category ? seed.filter(item => item.category === category) : seed;
    await route.fulfill({ json: { page: 1, pageSize: 12, total: items.length, items } });
  });
  await page.goto('/');
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(3);
  await page.getByRole('button', { name: /Красота/ }).click();
  await expect(page.getByRole('alert')).toContainText('Услугата е временно недостъпна.');
  await expect(page.locator('.results-count')).toHaveCount(0);
  online = true;
  await page.getByRole('button', { name: 'Опитай отново' }).click();
  await expect(page.getByRole('alert')).toHaveCount(0);
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(1);
  await expect(page.locator('.location-card').filter({ hasText: 'Студио „Линия“' })).toBeVisible();
});

test('network failure does not disclose local API topology to visitors', async ({ page }) => {
  await page.route('**/api/v1/homepage', route => route.abort('failed'));
  await page.goto('/');
  const error = page.getByRole('alert');
  await expect(error).toContainText('Няма връзка със сървъра.');
  await expect(error).not.toContainText(/127\.0\.0\.1|localhost|\.NET|Angular|proxy/i);
});

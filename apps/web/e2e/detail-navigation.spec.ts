import { expect, test } from '@playwright/test';

// Fictional demo data only. No live API, account, SMTP or tenant information.
const detail = {
  id: '11111111-1111-4111-8111-111111111111',
  slug: 'bike-demo',
  name: 'Вело сервиз „Педал“',
  city: 'Варна',
  category: 'Услуги',
  neighborhood: 'Левски',
  description: 'Примерен велосипедeн сервиз.',
  address: 'Демо адрес',
  imageUrl: null,
  isVerified: false,
  openingHours: '09:00–18:00',
  phone: null,
  website: null,
  updatedAtUtc: '2026-11-08T12:00:00Z',
  offers: [], brochures: [], jobs: []
};

test('detail 503 shows a visitor-facing error and recovers on retry without reloading', async ({ page }) => {
  let requests = 0;
  await page.route('**/api/v1/locations/bike-demo', route => {
    requests++;
    if (requests === 1) return route.fulfill({
      status: 503,
      contentType: 'application/json',
      body: JSON.stringify({
        ...detail,
        name: 'Остарял профил',
        description: 'Не показвай тази информация след неуспешен отговор. internal API / 127.0.0.1'
      })
    });
    return route.fulfill({ json: detail });
  });

  await page.goto('/obekt/bike-demo');
  const failure = page.getByRole('alert');
  await expect(failure).toContainText('Услугата е временно недостъпна');
  await expect(failure).not.toContainText(/API|127\.0\.0\.1|localhost|proxy|Angular/i);
  await expect(failure).not.toContainText('Остарял профил');
  await expect(page.getByText('Остарял профил')).toHaveCount(0);
  await expect(page.locator('.detail-hero')).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Опитай отново' })).toBeEnabled();

  await page.getByRole('button', { name: 'Опитай отново' }).click();
  await expect(page.getByRole('heading', { name: detail.name })).toBeVisible();
  await expect(page.locator('.detail-hero')).toHaveCount(1);
  await expect(page.getByRole('alert')).toHaveCount(0);
  await expect(page.locator('.detail-shell')).toHaveAttribute('aria-busy', 'false');
  expect(requests).toBe(2);
});

test('404 remains a distinct not-found state without a misleading retry', async ({ page }) => {
  await page.route('**/api/v1/locations/bike-demo', route =>
    route.fulfill({ status: 404, body: 'Not Found' })
  );
  await page.goto('/obekt/bike-demo');
  await expect(page.getByRole('alert')).toContainText('Обектът не е намерен или е скрит');
  await expect(page.getByRole('button', { name: 'Опитай отново' })).toHaveCount(0);
  await expect(page.getByRole('link', { name: /Към всички обекти/ })).toBeVisible();
  await expect(page.locator('.detail-hero')).toHaveCount(0);
});

test('detail loading announces progress and aria-busy clears after data arrives', async ({ page }) => {
  let releaseResponse: () => void = () => {};
  const holdResponse = new Promise<void>(resolve => { releaseResponse = resolve; });
  await page.route('**/api/v1/locations/bike-demo', async route => {
    await holdResponse;
    try { await route.fulfill({ json: detail }); } catch { /* Superseded navigation. */ }
  });
  await page.goto('/obekt/bike-demo');

  await expect(page.locator('.detail-shell')).toHaveAttribute('aria-busy', 'true');
  await expect(page.getByRole('status')).toContainText('Зареждаме информацията за обекта');
  expect(await page.locator('.detail-hero').count()).toBe(0);

  releaseResponse();
  await expect(page.getByRole('heading', { name: detail.name })).toBeVisible();
  await expect(page.locator('.detail-shell')).toHaveAttribute('aria-busy', 'false');
  // The three empty publication sections intentionally retain role=status announcements.
  await expect(page.locator('.detail-loading-card[role="status"]')).toHaveCount(0);
  await expect(page.locator('.detail-empty[role="status"]')).toHaveCount(3);
});

test('detail network errors remain generic and retryable', async ({ page }) => {
  let requests = 0;
  await page.route('**/api/v1/locations/bike-demo', route => {
    requests++;
    return requests === 1 ? route.abort('failed') : route.fulfill({ json: detail });
  });
  await page.goto('/obekt/bike-demo');
  await expect(page.getByRole('alert')).toContainText('Няма връзка със сървъра');
  await page.getByRole('button', { name: 'Опитай отново' }).click();
  await expect(page.getByRole('heading', { name: detail.name })).toBeVisible();
  expect(requests).toBe(2);
});

test('navigating away during a pending detail request never shows stale detail', async ({ page }) => {
  let releaseResponse: () => void = () => {};
  const holdResponse = new Promise<void>(resolve => { releaseResponse = resolve; });
  await page.route('**/api/v1/locations/bike-demo', async route => {
    await holdResponse;
    try { await route.fulfill({ json: detail }); } catch { /* Canceled by router. */ }
  });
  await page.route('**/api/v1/homepage', route => route.fulfill({
    json: {
      meta: { cities: ['Варна'], neighborhoods: [], categories: [] },
      directory: { page: 1, pageSize: 12, total: 0, items: [] },
      offers: [], brochures: [], jobs: []
    }
  }));
  await page.goto('/obekt/bike-demo');
  await expect(page.getByRole('status')).toBeVisible();
  await page.getByRole('link', { name: /Към всички обекти/ }).click();
  await expect(page.locator('.hero h1')).toBeVisible();
  releaseResponse();
  await expect(page.locator('.detail-hero')).toHaveCount(0);
  await expect(page.getByRole('heading', { name: detail.name })).toHaveCount(0);
});

test('mobile navigation closes on Escape and restores focus to menu toggle', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.route('**/api/v1/homepage', route => route.fulfill({
    json: {
      meta: { cities: ['Варна'], neighborhoods: [], categories: [] },
      directory: { page: 1, pageSize: 12, total: 0, items: [] },
      offers: [], brochures: [], jobs: []
    }
  }));
  await page.goto('/');
  const toggle = page.getByRole('button', { name: 'Отвори навигацията' });
  await expect(toggle).toBeVisible();
  await toggle.click();
  await expect(page.getByRole('button', { name: 'Затвори навигацията' })).toHaveAttribute('aria-expanded', 'true');
  await expect(page.locator('#primary-nav')).toBeVisible();

  await page.locator('#primary-nav a').first().focus();
  await page.keyboard.press('Escape');
  await expect(page.locator('#primary-nav')).toBeHidden();
  await expect(toggle).toBeFocused();
  await expect(toggle).toHaveAttribute('aria-expanded', 'false');

  await toggle.click();
  await page.getByRole('button', { name: 'Затвори навигацията' }).press('Escape');
  await expect(page.locator('#primary-nav')).toBeHidden();
  await expect(toggle).toBeFocused();
});

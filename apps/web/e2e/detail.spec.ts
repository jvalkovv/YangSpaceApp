import { expect, test } from '@playwright/test';

const detail = {
  id: '11111111-1111-4111-8111-111111111111',
  slug: 'bike-demo',
  name: 'Вело сервиз „Педал“',
  neighborhood: 'Левски',
  category: 'Услуги',
  description: 'Сервиз и поддръжка на велосипеди. Настройка, профилактика и ремонт.',
  address: 'ул. Примерна 12',
  imageUrl: null,
  isVerified: true,
  city: 'Варна',
  phone: '+359 888 123 456',
  website: 'https://example.test',
  openingHours: 'Понеделник – Петък, 09:00 – 18:00',
  updatedAtUtc: '2026-11-19T10:30:00Z',
  offers: [{
    id: 'offer-1',
    title: 'Пролетна профилактика',
    description: 'Проверка и настройка на основните компоненти.',
    startsAtUtc: '2026-11-01T08:00:00Z',
    endsAtUtc: '2026-12-01T18:00:00Z'
  }],
  brochures: [{
    id: 'brochure-1',
    title: 'Каталог с аксесоари',
    url: 'https://example.test/catalog.pdf',
    validFromUtc: '2026-11-01T08:00:00Z',
    validToUtc: '2026-12-01T18:00:00Z'
  }],
  jobs: [{
    id: 'job-1',
    title: 'Веломеханик',
    description: 'Търсим човек с интерес към велосипедите.',
    applyUrl: 'https://example.test/jobs',
    expiresAtUtc: '2026-12-15T18:00:00Z'
  }]
};

test('object detail renders the profile, active content and contact actions', async ({ page }) => {
  await page.route('**/api/v1/locations/bike-demo', route => route.fulfill({ json: detail }));
  await page.goto('/obekt/bike-demo');

  await expect(page.getByRole('heading', { level: 1, name: 'Вело сервиз „Педал“' })).toBeVisible();
  await expect(page.getByText('Проверен профил')).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Предложения' })).toBeVisible();
  await expect(page.getByText('Пролетна профилактика')).toBeVisible();
  await expect(page.getByText('Каталог с аксесоари')).toBeVisible();
  await expect(page.getByRole('link', { name: /Отвори брошурата/ })).toHaveAttribute('href', 'https://example.test/catalog.pdf');
  await expect(page.getByText('Веломеханик')).toBeVisible();
  await expect(page.getByRole('link', { name: /Обади се/ })).toHaveAttribute('href', 'tel:+359 888 123 456');
  await expect(page.getByRole('link', { name: /Уебсайт/ })).toHaveAttribute('href', 'https://example.test');
});

test('empty offers, brochures and jobs are presented as genuine empty states', async ({ page }) => {
  await page.route('**/api/v1/locations/empty-demo', route => route.fulfill({
    json: {
      ...detail,
      slug: 'empty-demo',
      offers: [],
      brochures: [],
      jobs: []
    }
  }));
  await page.goto('/obekt/empty-demo');

  await expect(page.locator('.detail-empty')).toHaveCount(3);
  await expect(page.getByText('Няма активни предложения.')).toBeVisible();
  await expect(page.getByText('Няма активни брошури.')).toBeVisible();
  await expect(page.getByText('В момента няма активни позиции.')).toBeVisible();
});

test('detail loading state is announced before the profile response arrives', async ({ page }) => {
  await page.route('**/api/v1/locations/loading-demo', async route => {
    await new Promise(resolve => setTimeout(resolve, 180));
    await route.fulfill({ json: { ...detail, slug: 'loading-demo' } });
  });
  await page.goto('/obekt/loading-demo');

  await expect(page.getByRole('status')).toContainText('Зареждаме информацията');
  await expect(page.getByRole('status')).toHaveAttribute('aria-busy', 'true');
  await expect(page.getByRole('heading', { level: 1, name: 'Вело сервиз „Педал“' })).toBeVisible();
});

test('HTTP 503 is unavailable rather than not-found and retry recovers the detail', async ({ page }) => {
  let attempts = 0;
  await page.route('**/api/v1/locations/retry-demo', route => {
    attempts += 1;
    return attempts === 1
      ? route.fulfill({
          status: 503,
          contentType: 'application/json',
          body: JSON.stringify({ ...detail, slug: 'retry-demo', name: 'Остарял профил' })
        })
      : route.fulfill({ json: { ...detail, slug: 'retry-demo' } });
  });
  await page.goto('/obekt/retry-demo');

  await expect(page.getByRole('alert')).toContainText('В момента не можем да заредим профила');
  await expect(page.getByRole('alert')).toContainText('Услугата е временно недостъпна');
  await expect(page.getByRole('heading', { name: 'Не открихме този обект.' })).toHaveCount(0);
  await expect(page.getByText('Остарял профил')).toHaveCount(0);

  await page.getByRole('button', { name: /Опитай отново/ }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Вело сервиз „Педал“' })).toBeVisible();
  expect(attempts).toBe(2);
});

test('network failure has a retryable connection state', async ({ page }) => {
  let attempts = 0;
  await page.route('**/api/v1/locations/network-demo', route => {
    attempts += 1;
    return attempts === 1
      ? route.abort('failed')
      : route.fulfill({ json: { ...detail, slug: 'network-demo' } });
  });
  await page.goto('/obekt/network-demo');

  await expect(page.getByRole('alert')).toContainText('Няма връзка със сървъра');
  await page.getByRole('button', { name: /Опитай отново/ }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Вело сервиз „Педал“' })).toBeVisible();
  expect(attempts).toBe(2);
});

test('HTTP 404 is a distinct not-found state and does not offer a misleading retry', async ({ page }) => {
  await page.route('**/api/v1/locations/missing-demo', route => route.fulfill({
    status: 404,
    contentType: 'application/problem+json',
    body: JSON.stringify({ title: 'Not Found', status: 404 })
  }));
  await page.goto('/obekt/missing-demo');

  const state = page.getByRole('alert');
  await expect(state).toContainText('Не открихме този обект');
  await expect(state).toContainText('не е намерен или вече не е публично достъпен');
  await expect(state.getByRole('link', { name: /Към каталога/ })).toHaveAttribute('href', '/');
  await expect(state.getByRole('button', { name: /Опитай отново/ })).toHaveCount(0);
});

test('a late response from a previous slug cannot replace the current object', async ({ page }) => {
  let slowStarted = false;
  let slowFinished = false;

  await page.route('**/api/v1/locations/slow-demo', async route => {
    slowStarted = true;
    try {
      await new Promise(resolve => setTimeout(resolve, 250));
      await route.fulfill({ json: { ...detail, slug: 'slow-demo', name: 'Старият отговор' } });
    } catch {
      // Angular HttpClient may abort the previous request when switchMap changes route.
    } finally {
      slowFinished = true;
    }
  });
  await page.route('**/api/v1/locations/fast-demo', route => route.fulfill({
    json: { ...detail, slug: 'fast-demo', name: 'Актуалният обект' }
  }));

  await page.goto('/obekt/slow-demo');
  await expect.poll(() => slowStarted).toBe(true);

  await page.evaluate(() => {
    window.history.pushState({}, '', '/obekt/fast-demo');
    window.dispatchEvent(new PopStateEvent('popstate'));
  });

  await expect(page.getByRole('heading', { level: 1, name: 'Актуалният обект' })).toBeVisible();
  await expect.poll(() => slowFinished).toBe(true);
  await expect(page.getByRole('heading', { level: 1, name: 'Актуалният обект' })).toBeVisible();
  await expect(page.getByText('Старият отговор')).toHaveCount(0);
});

for (const width of [320, 360, 375, 390, 430, 768, 820, 1024, 1280, 1440]) {
  test(`object detail is usable without horizontal overflow at ${width}px`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width, height: 900 });
    await page.route('**/api/v1/locations/bike-demo', route => route.fulfill({ json: detail }));
    await page.goto('/obekt/bike-demo');

    await expect(page.getByRole('heading', { level: 1, name: 'Вело сервиз „Педал“' })).toBeVisible();
    await expect(page.locator('.detail-grid')).toBeVisible();
    await expect(page.locator('.detail-sidebar')).toBeVisible();

    const metrics = await page.evaluate(() => {
      const viewport = window.innerWidth;
      const root = document.documentElement.getBoundingClientRect();
      const body = document.body.getBoundingClientRect();
      const actions = [...document.querySelectorAll('.detail-contact-actions .btn')].map(element => {
        const rect = element.getBoundingClientRect();
        return { left: rect.left, right: rect.right, width: rect.width };
      });
      return {
        viewport,
        rootWidth: document.documentElement.scrollWidth,
        bodyWidth: document.body.scrollWidth,
        rootLeft: root.left,
        bodyRight: body.right,
        actions
      };
    });

    expect(metrics.rootWidth, 'document must not overflow horizontally').toBeLessThanOrEqual(width + 1);
    expect(metrics.bodyWidth, 'body must not overflow horizontally').toBeLessThanOrEqual(width + 1);
    for (const action of metrics.actions) {
      expect(action.left).toBeGreaterThanOrEqual(-1);
      expect(action.right).toBeLessThanOrEqual(width + 1);
      expect(action.width).toBeGreaterThan(0);
    }

    await page.screenshot({
      path: testInfo.outputPath(`yangspace-detail-${width}.png`),
      fullPage: true,
      animations: 'disabled'
    });
  });
}

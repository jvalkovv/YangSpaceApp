import { expect, test } from '@playwright/test';

const widths = [320, 360, 375, 390, 430, 768, 820, 1024, 1280, 1440] as const;

const detail = {
  id: '11111111-1111-4111-8111-111111111111',
  slug: 'bike-demo',
  name: 'Вело сервиз „Педал“',
  city: 'Варна',
  category: 'Услуги',
  neighborhood: 'Левски',
  description: 'Това е примерна информация за сервиз и поддръжка на велосипеди.',
  address: 'Примерен адрес, Варна',
  imageUrl: null,
  isVerified: false,
  openingHours: '09:00–18:00',
  phone: null,
  website: null,
  updatedAtUtc: '2026-11-08T12:00:00Z',
  offers: [{
    id: '22222222-2222-4222-8222-222222222222',
    title: 'Примерна сезонна оферта',
    description: 'Демо информация без търговска валидност.',
    startsAtUtc: '2026-11-09T09:00:00Z',
    endsAtUtc: '2026-12-08T09:00:00Z'
  }],
  brochures: [{
    id: '33333333-3333-4333-8333-333333333333',
    title: 'Демо брошура',
    url: 'https://example.org/brochure.pdf',
    validFromUtc: '2026-11-09T09:00:00Z',
    validToUtc: '2026-12-08T09:00:00Z'
  }],
  jobs: [{
    id: '44444444-4444-4444-8444-444444444444',
    title: 'Тестова позиция',
    description: 'Тестов текст',
    applyUrl: 'https://example.org/jobs',
    expiresAtUtc: '2026-12-08T09:00:00Z'
  }]
};

const adminLocation = {
  ...detail,
  isPublished: false,
  description: detail.description
};

async function verifyLayout(page: import('@playwright/test').Page, selectors: string[], width: number): Promise<void> {
  const layout = await page.evaluate((queries) => {
    const elements = queries.flatMap(query =>
      [...document.querySelectorAll<HTMLElement>(query)].filter(element =>
        !!element.getClientRects().length && getComputedStyle(element).visibility !== 'hidden'
      )
    );
    const blocks = elements.map(element => {
      const box = element.getBoundingClientRect();
      return {
        selector: element.className || element.tagName,
        left: box.left, right: box.right, width: box.width
      };
    });
    return {
      viewport: window.innerWidth,
      document: document.documentElement.scrollWidth,
      body: document.body.scrollWidth,
      blocks
    };
  }, selectors);

  expect(layout.viewport).toBe(width);
  expect(layout.document, 'document must not scroll horizontally').toBeLessThanOrEqual(width + 1);
  expect(layout.body, 'body must not scroll horizontally').toBeLessThanOrEqual(width + 1);
  for (const block of layout.blocks) {
    expect(block.width, `visible control ${block.selector} must have width`).toBeGreaterThan(0);
    expect(block.left, `clipped left: ${block.selector}`).toBeGreaterThanOrEqual(-1);
    expect(block.right, `clipped right: ${block.selector}`).toBeLessThanOrEqual(width + 1);
  }
}

for (const width of widths) {
  test(`object detail is usable at ${width}px`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width, height: 900 });
    await page.route('**/api/v1/locations/bike-demo', route => route.fulfill({ json: detail }));
    await page.goto('/obekt/bike-demo');
    await expect(page.getByRole('heading', { name: detail.name })).toBeVisible();
    await expect(page.locator('.detail-main .panel')).toHaveCount(3);
    await expect(page.locator('.detail-sidebar')).toBeVisible();
    await verifyLayout(page, [
      '.detail-hero', '.detail-grid', '.detail-main .panel', '.detail-sidebar',
      '.detail-main .offer-line', '.detail-info'
    ], width);
    await page.screenshot({
      path: testInfo.outputPath(`yangspace-detail-${width}.png`),
      fullPage: true,
      animations: 'disabled'
    });
  });

  test(`preview admin workspace and editor fit ${width}px`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width, height: 900 });
    await page.route('**/api/auth/login', route => route.fulfill({
      json: { accessToken: 'mock-h06-2-platform-admin', tokenType: 'Bearer', expiresIn: 3600, refreshToken: 'unused' }
    }));
    await page.route('**/api/v1/admin/me', route => route.fulfill({
      json: { email: 'qa@example.invalid', role: 'PlatformAdmin' }
    }));
    await page.route('**/api/v1/admin/locations', route => route.fulfill({
      json: [adminLocation]
    }));
    await page.route('**/api/v1/admin/locations/**', route => route.fulfill({
      json: { offers: [], brochures: [], jobs: [] }
    }));
    await page.goto('/admin');
    await page.getByLabel('Имейл').fill('qa@example.invalid');
    await page.getByLabel('Парола').fill('DisposableTestPassword123!');
    await page.getByRole('button', { name: 'Вход' }).click();

    await expect(page.locator('.admin-workspace')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Обекти (1)' })).toBeVisible();

    await page.getByRole('button', { name: 'Редактирай' }).click();
    await expect(page.locator('.admin-edit')).toBeVisible();
    await verifyLayout(page, [
      '.admin-workspace', '.admin-columns > .panel', '.admin-list',
      '.admin-list-row', '.admin-edit', '.admin-edit-fields input',
      '.admin-actions', '.admin-edit-actions'
    ], width);
    await page.screenshot({
      path: testInfo.outputPath(`yangspace-admin-${width}.png`),
      fullPage: true,
      animations: 'disabled'
    });
  });
}

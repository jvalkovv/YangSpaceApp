import { expect, test } from '@playwright/test';

const business = {
  id: '11111111-1111-4111-8111-111111111111',
  slug: 'bakery-demo',
  name: 'Пекарна „Сутрин“',
  neighborhood: 'Левски',
  category: 'Пекарни',
  description: 'Прясно изпечен хляб и закуски.',
  address: 'Демо адрес, Варна',
  imageUrl: 'https://images.example.test/bakery.svg',
  isVerified: true,
  activeOffers: 1,
  activeJobs: 1
};

function homepage(overrides: Record<string, unknown> = {}) {
  return {
    meta: {
      cities: ['Варна'],
      neighborhoods: ['Левски'],
      categories: ['Пекарни']
    },
    directory: { items: [business], page: 1, pageSize: 12, total: 1 },
    offers: [{
      slug: business.slug,
      locationName: business.name,
      neighborhood: business.neighborhood,
      category: business.category,
      title: 'Оферта за закуска',
      expiresAtUtc: '2026-12-31T23:59:00Z'
    }],
    brochures: [],
    jobs: [{
      slug: business.slug,
      locationName: business.name,
      neighborhood: business.neighborhood,
      category: business.category,
      title: 'Помощник-пекар',
      expiresAtUtc: '2026-12-31T23:59:00Z'
    }],
    ...overrides
  };
}

async function mockHomepage(page: import('@playwright/test').Page, payload = homepage()): Promise<void> {
  await page.route('**/api/v1/homepage', route => route.fulfill({ json: payload }));
}

test('mobile discovery order puts search before categories and directory results', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await mockHomepage(page);
  await page.goto('/');

  await expect(page.locator('.discovery-search .search-panel')).toBeVisible();
  const order = await page.locator('.discovery-search, .quick-discovery, .catalog-wrap')
    .evaluateAll(nodes => nodes.map(node => node.classList[0]));
  expect(order).toEqual(['discovery-search', 'quick-discovery', 'catalog-wrap']);

  const search = await page.locator('.discovery-search').boundingBox();
  const categories = await page.locator('.quick-discovery').boundingBox();
  const catalogue = await page.locator('.catalog-wrap').boundingBox();
  expect(search).not.toBeNull();
  expect(categories).not.toBeNull();
  expect(catalogue).not.toBeNull();
  expect(search!.y).toBeLessThan(categories!.y);
  expect(categories!.y).toBeLessThan(catalogue!.y);
});

test('directory card renders supplied business photography and verified state', async ({ page }) => {
  await page.route('**/api/v1/homepage', route => route.fulfill({ json: homepage() }));
  await page.route('**/bakery.svg', route => route.fulfill({
    status: 200,
    contentType: 'image/svg+xml',
    body: '<svg xmlns="http://www.w3.org/2000/svg" width="640" height="360"><rect width="640" height="360" fill="#d7e7d5"/></svg>'
  }));
  await page.goto('/');

  const card = page.locator('.location-card').filter({ hasText: business.name });
  const image = card.locator('.card-cover-image');
  await expect(image).toHaveAttribute('alt', 'Снимка на ' + business.name);
  await image.scrollIntoViewIfNeeded();
  await expect.poll(() => image.evaluate(node => (node as HTMLImageElement).naturalWidth)).toBe(640);
  await expect(card.locator('.cover-verified')).toContainText('Проверен');
  await expect(card.locator('.cover-verified')).toHaveAttribute('aria-label', 'Проверен профил');
  await expect(card.locator('.cover-symbol')).toHaveCount(0);
});

test('broken business image falls back to the category illustration', async ({ page }) => {
  await page.route('**/api/v1/homepage', route => route.fulfill({ json: homepage() }));
  await page.route('**/bakery.svg', route => route.fulfill({ status: 404, body: 'not found' }));
  await page.goto('/');

  const card = page.locator('.location-card').filter({ hasText: business.name });
  await card.scrollIntoViewIfNeeded();
  await expect(card.locator('.cover-symbol')).toBeVisible();
  await expect(card.locator('.card-cover-image')).toHaveCount(0);
});

test('inconsistent publication counters are not presented as genuine empty states and retry recovers', async ({ page }) => {
  let attempts = 0;
  await page.route('**/api/v1/homepage', route => {
    attempts += 1;
    const payload = attempts === 1
      ? homepage({ offers: [], jobs: [] })
      : homepage();
    return route.fulfill({ json: payload });
  });
  await page.goto('/');

  const offers = page.locator('.discover-offers');
  const jobs = page.locator('.discover-jobs');
  await expect(offers.locator('.discover-unavailable')).toContainText('Данните за офертите не са съгласувани');
  await expect(jobs.locator('.discover-unavailable')).toContainText('Данните за публикациите не са съгласувани');
  await expect(offers).not.toContainText('Няма активни предложения в момента.');
  await expect(jobs).not.toContainText('Няма активни позиции в момента.');
  await expect(page.locator('.discover-brochures')).toContainText('Няма активни брошури в момента.');
  await expect(page.locator('.discover-brochures')).toContainText('Няма активни брошури в момента.');

  await offers.getByRole('button', { name: 'Повтори зареждането на офертите' }).click();
  await expect(offers.locator('.discover-item')).toContainText('Оферта за закуска');
  await expect(jobs.locator('.discover-item')).toContainText('Помощник-пекар');
  await expect(offers.locator('.discover-unavailable')).toHaveCount(0);
  await expect(jobs.locator('.discover-unavailable')).toHaveCount(0);
  expect(attempts).toBe(2);
});

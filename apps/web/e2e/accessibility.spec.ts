import { expect, test } from '@playwright/test';

const homepage = {
  meta: { cities: ['Варна'], neighborhoods: ['Левски'], categories: ['Заведения'] },
  directory: { items: [], page: 1, pageSize: 12, total: 0 },
  offers: [], brochures: [], jobs: []
};

const detail = {
  id: '11111111-1111-4111-8111-111111111111',
  slug: 'accessibility-demo',
  name: 'Кафене „Ъгълът“',
  city: 'Варна',
  category: 'Заведения',
  neighborhood: 'Левски',
  description: 'Примерна информация за обекта.',
  address: 'Демо адрес',
  imageUrl: null,
  isVerified: false,
  openingHours: null,
  phone: null,
  website: null,
  updatedAtUtc: '2026-11-19T10:30:00Z',
  offers: [],
  brochures: [],
  jobs: []
};

test('skip link is the first keyboard stop and directory controls have accessible names', async ({ page }) => {
  await page.route('**/api/v1/homepage', route => route.fulfill({ json: homepage }));
  await page.route('**/api/v1/locations?*', route => route.fulfill({
    json: { items: [], page: 1, pageSize: 12, total: 0 }
  }));
  await page.goto('/');

  await page.keyboard.press('Tab');
  const skip = page.getByRole('link', { name: 'Към основното съдържание' });
  await expect(skip).toBeFocused();
  const focusStyle = await skip.evaluate(element => ({
    top: getComputedStyle(element).top,
    outlineStyle: getComputedStyle(element).outlineStyle,
    outlineWidth: getComputedStyle(element).outlineWidth
  }));
  expect(focusStyle.top).toBe('10px');
  expect(focusStyle.outlineStyle).not.toBe('none');
  expect(Number.parseFloat(focusStyle.outlineWidth)).toBeGreaterThan(0);

  await expect(page.getByRole('search', { name: 'Търсене на търговски обекти' })).toBeVisible();
  await expect(page.getByRole('searchbox', { name: 'Име на обект или ключова дума' })).toBeVisible();
  await expect(page.getByRole('combobox', { name: 'Избери квартал' })).toBeVisible();
  await expect(page.getByRole('combobox', { name: 'Избери категория' })).toBeVisible();

  const category = page.getByRole('button', { name: /Заведения/ });
  await expect(category).toHaveAttribute('aria-pressed', 'false');
  await category.click();
  await expect(category).toHaveAttribute('aria-pressed', 'true');
});

test('detail has one main landmark and respects reduced-motion preference', async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  let releaseResponse: () => void = () => {};
  const holdResponse = new Promise<void>(resolve => { releaseResponse = resolve; });

  await page.route('**/api/v1/locations/accessibility-demo', async route => {
    await holdResponse;
    try { await route.fulfill({ json: detail }); } catch { /* Route may be canceled on teardown. */ }
  });
  await page.goto('/obekt/accessibility-demo');

  await expect(page.getByRole('status')).toBeVisible();
  await expect(page.locator('main')).toHaveCount(1);
  await expect(page.locator('h1')).toHaveCount(1);
  const animationName = await page.locator('.detail-spinner').evaluate(element =>
    getComputedStyle(element).animationName
  );
  expect(animationName).toBe('none');

  releaseResponse();
  await expect(page.getByRole('heading', { level: 1, name: detail.name })).toBeVisible();
  await expect(page.locator('main')).toHaveCount(1);
});

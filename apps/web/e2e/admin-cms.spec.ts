import { expect, test } from '@playwright/test';

const id = '55555555-5555-4555-8555-555555555555';
const initialLocation = {
  id,
  slug: 'ci-service',
  name: 'Примерен сервиз',
  city: 'Варна',
  neighborhood: 'Левски',
  category: 'Услуги',
  address: 'Демо адрес 1',
  description: 'Тестов обект без реални лични данни.',
  phone: null,
  website: null,
  imageUrl: null,
  openingHours: '09:00–18:00',
  isPublished: false,
  isVerified: false,
  updatedAtUtc: '2026-11-09T00:00:00Z'
};

test('preview login must verify PlatformAdmin before displaying CMS', async ({ page }) => {
  await page.route('**/api/auth/login', route => route.fulfill({
    json: { accessToken: 'ordinary-member-token', tokenType: 'Bearer', expiresIn: 3600 }
  }));
  await page.route('**/api/v1/admin/me', route => route.fulfill({
    status: 403, json: { message: 'Forbidden' }
  }));
  await page.goto('/admin');
  await page.getByLabel('Имейл').fill('reader@example.invalid');
  await page.getByLabel('Парола').fill('DisposableTestPassword123!');
  await page.getByRole('button', { name: 'Вход' }).click();
  await expect(page.locator('.admin-workspace')).toHaveCount(0);
  await expect(page.getByRole('alert')).toContainText('администраторската роля не е потвърдена');
});

test('preview CMS edits and toggles location publication with backend roundtrips', async ({ page }) => {
  let stored = { ...initialLocation };
  const audit: string[] = [];
  await page.route('**/api/auth/login', route => route.fulfill({
    json: { accessToken: 'mock-platform-admin', tokenType: 'Bearer', expiresIn: 3600 }
  }));
  await page.route('**/api/v1/admin/me', route => route.fulfill({
    json: { email: 'admin@example.invalid', role: 'PlatformAdmin' }
  }));
  await page.route('**/api/v1/admin/locations', route => {
    audit.push('LIST');
    return route.fulfill({ json: [stored] });
  });
  await page.route('**/api/v1/admin/locations/*', async route => {
    const request = route.request();
    const target = new URL(request.url()).pathname;
    if (request.method() === 'PUT' && target.endsWith('/' + id)) {
      const edited = request.postDataJSON();
      stored = { ...stored, ...edited };
      audit.push('EDIT');
      await route.fulfill({ json: { id, slug: stored.slug } });
      return;
    }
    if (request.method() === 'POST' && target.endsWith('/' + id + '/restore')) {
      stored.isPublished = true;
      audit.push('RESTORE');
      await route.fulfill({ status: 204 });
      return;
    }
    if (request.method() === 'POST' && target.endsWith('/' + id + '/hide')) {
      stored.isPublished = false;
      audit.push('HIDE');
      await route.fulfill({ status: 204 });
      return;
    }
    await route.fulfill({ status: 404 });
  });

  await page.goto('/admin');
  await page.getByLabel('Имейл').fill('admin@example.invalid');
  await page.getByLabel('Парола').fill('DisposableTestPassword123!');
  await page.getByRole('button', { name: 'Вход' }).click();
  await expect(page.getByRole('heading', { name: 'Обекти (1)' })).toBeVisible();
  await expect(page.locator('.admin-list-row')).toContainText('Скрит');

  await page.getByRole('button', { name: 'Редактирай' }).click();
  const editor = page.locator('.admin-edit');
  await expect(editor).toBeVisible();
  await editor.getByLabel('Име').fill('Примерен сервиз — редактиран');
  await editor.getByLabel('Адрес').fill('Демо адрес 2');
  await editor.getByRole('button', { name: 'Запази промените' }).click();

  await expect(editor).toHaveCount(0);
  await expect(page.locator('.admin-list-row')).toContainText('Примерен сервиз — редактиран');
  expect(stored.address).toBe('Демо адрес 2');
  expect(audit).toContain('EDIT');

  await page.getByRole('button', { name: 'Публикувай', exact: true }).click();
  await expect(page.locator('.admin-list-row')).toContainText('Публичен');
  expect(stored.isPublished).toBe(true);
  await page.getByRole('button', { name: 'Скрий' }).click();
  await expect(page.locator('.admin-list-row')).toContainText('Скрит');
  expect(stored.isPublished).toBe(false);
  expect(audit).toEqual(expect.arrayContaining(['RESTORE', 'HIDE']));
});

test('CMS duplicate slug keeps the unsaved edit visible', async ({ page }) => {
  await page.route('**/api/auth/login', route => route.fulfill({
    json: { accessToken: 'mock-platform-admin', tokenType: 'Bearer', expiresIn: 3600 }
  }));
  await page.route('**/api/v1/admin/me', route => route.fulfill({ json: { role: 'PlatformAdmin' } }));
  await page.route('**/api/v1/admin/locations', route => route.fulfill({ json: [initialLocation] }));
  await page.route('**/api/v1/admin/locations/*', route => route.fulfill({ status: 409, json: { error: 'Slug already exists.' } }));
  await page.goto('/admin');
  await page.getByLabel('Имейл').fill('admin@example.invalid');
  await page.getByLabel('Парола').fill('DisposableTestPassword123!');
  await page.getByRole('button', { name: 'Вход' }).click();
  await expect(page.locator('.admin-list-row')).toHaveCount(1);
  await page.getByRole('button', { name: 'Редактирай' }).click();
  const editor = page.locator('.admin-edit');
  await editor.getByLabel('Slug').fill('duplicate-slug');
  await editor.getByRole('button', { name: 'Запази промените' }).click();
  await expect(page.getByRole('alert')).toContainText('Slug вече се използва');
  await expect(editor.getByLabel('Slug')).toHaveValue('duplicate-slug');
});

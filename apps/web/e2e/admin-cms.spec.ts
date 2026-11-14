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
  await page.route('**/api/v1/admin/locations/**', async route => {
    const request = route.request();
    const target = new URL(request.url()).pathname;
    if (request.method() === 'GET' && target.endsWith('/' + id + '/content')) {
      await route.fulfill({ json: { offers: [], brochures: [], jobs: [] } });
      return;
    }
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


test('publication CMS edits, archives and restores within selected location', async ({ page }) => {
  const future = (days: number) => new Date(Date.now() + days * 86400000).toISOString();
  const offerId = '77777777-7777-4777-8777-777777777777';
  const brochureId = '88888888-8888-4888-8888-888888888888';
  const jobId = '99999999-9999-4999-8999-999999999999';
  const content = {
    offers: [{
      id: offerId, locationId: id, title: 'Тестова оферта', description: 'Начален текст',
      startsAtUtc: future(-1), endsAtUtc: future(12), isPublished: true
    }],
    brochures: [{
      id: brochureId, locationId: id, title: 'Тестова брошура',
      url: 'https://example.org/catalog.pdf',
      validFromUtc: future(-1), validToUtc: future(15), isPublished: true
    }],
    jobs: [{
      id: jobId, locationId: id, title: 'Тестова позиция', description: 'Описание',
      applyUrl: 'https://example.org/apply', expiresAtUtc: future(30), isPublished: true
    }]
  };
  const operations: string[] = [];
  await page.route('**/api/auth/login', route => route.fulfill({
    json: { accessToken: 'temporary-platform-admin', tokenType: 'Bearer', expiresIn: 3600 }
  }));
  await page.route('**/api/v1/admin/me', route => route.fulfill({
    json: { role: 'PlatformAdmin' }
  }));
  await page.route('**/api/v1/admin/locations', route => route.fulfill({ json: [initialLocation] }));
  await page.route('**/api/v1/admin/locations/**', async route => {
    const req = route.request();
    const url = new URL(req.url()).pathname;
    if (req.method() === 'GET' && url.endsWith('/content')) {
      return route.fulfill({ json: content });
    }
    const match = url.match(/\/(offers|brochures|jobs)\/([0-9a-f-]+)(?:\/(hide|restore))?$/);
    if (!match) return route.fulfill({ status: 404 });
    const kind = match[1] as 'offers' | 'brochures' | 'jobs';
    const collection = kind === 'offers' ? content.offers : kind === 'brochures' ? content.brochures : content.jobs;
    const item = collection.find(value => value.id === match[2]);
    if (!item) return route.fulfill({ status: 404 });
    if (req.method() === 'PUT') {
      Object.assign(item, req.postDataJSON());
      operations.push('EDIT:' + kind);
      return route.fulfill({ status: 204 });
    }
    if (req.method() === 'POST' && match[3] === 'hide') {
      item.isPublished = false;
      operations.push('HIDE:' + kind);
      return route.fulfill({ status: 204 });
    }
    if (req.method() === 'POST' && match[3] === 'restore') {
      item.isPublished = true;
      operations.push('RESTORE:' + kind);
      return route.fulfill({ status: 204 });
    }
    return route.fulfill({ status: 405 });
  });

  await page.goto('/admin');
  await page.getByLabel('Имейл').fill('admin@example.invalid');
  await page.getByLabel('Парола').fill('DisposableTestPassword123!');
  await page.getByRole('button', { name: 'Вход' }).click();
  await expect(page.getByRole('heading', { name: 'Обекти (1)' })).toBeVisible();
  await page.getByRole('combobox', { name: 'Обект' }).selectOption(id);
  const panel = page.locator('ys-admin-publications');
  await expect(panel.getByText('Тестова оферта')).toBeVisible();
  await expect(panel.getByText('Тестова брошура')).toBeVisible();
  await expect(panel.getByText('Тестова позиция')).toBeVisible();

  const offerRow = panel.locator('.pub-row').filter({ hasText: 'Тестова оферта' });
  await offerRow.getByRole('button', { name: 'Редактирай' }).click();
  const editor = panel.locator('.pub-editor');
  await editor.getByLabel('Заглавие').fill('Редактирана оферта');
  await editor.getByRole('button', { name: 'Запази публикацията' }).click();
  await expect(panel.getByText('Редактирана оферта')).toBeVisible();

  const editedRow = panel.locator('.pub-row').filter({ hasText: 'Редактирана оферта' });
  await editedRow.getByRole('button', { name: 'Архивирай' }).click();
  await expect(editedRow).toContainText('Архивирана');
  await editedRow.getByRole('button', { name: 'Възстанови' }).click();
  await expect(editedRow).toContainText('Активна');

  const brochureRow = panel.locator('.pub-row').filter({ hasText: 'Тестова брошура' });
  await brochureRow.getByRole('button', { name: 'Архивирай' }).click();
  await expect(brochureRow).toContainText('Архивирана');

  const jobRow = panel.locator('.pub-row').filter({ hasText: 'Тестова позиция' });
  await jobRow.getByRole('button', { name: 'Редактирай' }).click();
  await editor.getByLabel('Заглавие').fill('Актуална позиция');
  await editor.getByRole('button', { name: 'Запази публикацията' }).click();
  await expect(panel.getByText('Актуална позиция')).toBeVisible();
  expect(operations).toEqual(expect.arrayContaining([
    'EDIT:offers', 'HIDE:offers', 'RESTORE:offers', 'HIDE:brochures', 'EDIT:jobs'
  ]));
});

test('publication CMS never accepts an expired archived record as directly restorable', async ({ page }) => {
  const oldOffer = {
    id: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
    locationId: id, title: 'Стара оферта', description: 'Архив',
    startsAtUtc: '2025-05-01T12:00:00Z', endsAtUtc: '2025-05-10T12:00:00Z',
    isPublished: false
  };
  await page.route('**/api/auth/login', route => route.fulfill({
    json: { accessToken: 'temporary-platform-admin', tokenType: 'Bearer', expiresIn: 3600 }
  }));
  await page.route('**/api/v1/admin/me', route => route.fulfill({ json: { role: 'PlatformAdmin' } }));
  await page.route('**/api/v1/admin/locations', route => route.fulfill({ json: [initialLocation] }));
  await page.route('**/api/v1/admin/locations/**', route => {
    const request = route.request();
    const target = new URL(request.url()).pathname;
    if (request.method() === 'GET' && target === '/api/v1/admin/locations/' + id + '/content') {
      return route.fulfill({ json: { offers: [oldOffer], brochures: [], jobs: [] } });
    }
    return route.fulfill({ status: 404 });
  });
  await page.goto('/admin');
  await page.getByLabel('Имейл').fill('admin@example.invalid');
  await page.getByLabel('Парола').fill('DisposableTestPassword123!');
  await page.getByRole('button', { name: 'Вход' }).click();
  await page.getByRole('combobox', { name: 'Обект' }).selectOption(id);
  const row = page.locator('.pub-row').filter({ hasText: 'Стара оферта' });
  await expect(row.getByRole('button', { name: 'Възстанови' })).toBeDisabled();
  await row.getByRole('button', { name: 'Редактирай' }).click();
  await expect(page.locator('.pub-editor')).toBeVisible();
});

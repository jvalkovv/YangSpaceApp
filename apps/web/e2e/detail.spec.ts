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

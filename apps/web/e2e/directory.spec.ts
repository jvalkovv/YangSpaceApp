import { expect, test } from '@playwright/test';

function location(index: number, name: string, category: string, neighborhood: string) {
  return {
    id: `00000000-0000-4000-8000-${String(index).padStart(12, '0')}`,
    slug: `demo-${index}`,
    name,
    category,
    neighborhood,
    description: 'Примерна информация за местния обект.',
    address: 'Демо адрес, Варна',
    imageUrl: null,
    isVerified: false,
    activeOffers: 0,
    activeJobs: 0
  };
}

const locations = [
  location(1, 'Вело сервиз „Педал“', 'Услуги', 'Левски'),
  location(2, 'Кафене „Ъгълът“', 'Заведения', 'Левски'),
  location(3, 'Ресторант „Морски бриз“', 'Заведения', 'Чайка')
];

const homepage = {
  meta: {
    cities: ['Варна'],
    neighborhoods: ['Левски', 'Чайка'],
    categories: ['Услуги', 'Заведения']
  },
  directory: { items: locations, page: 1, pageSize: 12, total: locations.length },
  offers: [],
  brochures: [],
  jobs: []
};

async function mockHomepage(page: import('@playwright/test').Page): Promise<void> {
  await page.route('**/api/v1/homepage', route => route.fulfill({ json: homepage }));
}

test('directory search submits the current query and renders matching results', async ({ page }) => {
  const requests: URL[] = [];
  await mockHomepage(page);
  await page.route('**/api/v1/locations?*', route => {
    const url = new URL(route.request().url());
    requests.push(url);
    const query = url.searchParams.get('q') ?? '';
    const items = query === 'Кафене' ? [locations[1]] : locations;
    return route.fulfill({
      json: { items, page: Number(url.searchParams.get('page') ?? 1), pageSize: 12, total: items.length }
    });
  });

  await page.goto('/');
  await page.locator('input[name="q"]').fill('Кафене');
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(1);
  await expect(page.locator('.location-card')).toContainText('Кафене „Ъгълът“');
  expect(requests.some(url => url.searchParams.get('q') === 'Кафене')).toBe(true);
});

test('category chip and neighborhood filter are combined in the directory request', async ({ page }) => {
  const requests: URL[] = [];
  await mockHomepage(page);
  await page.route('**/api/v1/locations?*', route => {
    const url = new URL(route.request().url());
    requests.push(url);
    const category = url.searchParams.get('category') ?? '';
    const neighborhood = url.searchParams.get('neighborhood') ?? '';
    const items = locations.filter(item =>
      (!category || item.category === category) &&
      (!neighborhood || item.neighborhood === neighborhood)
    );
    return route.fulfill({
      json: { items, page: Number(url.searchParams.get('page') ?? 1), pageSize: 12, total: items.length }
    });
  });

  await page.goto('/');
  await page.getByRole('button', { name: /Заведения/ }).click();
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(2);

  await page.locator('select[name="neighborhood"]').selectOption('Левски');
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(1);
  await expect(page.locator('.location-card')).toContainText('Кафене „Ъгълът“');

  const combined = requests.at(-1);
  expect(combined?.searchParams.get('category')).toBe('Заведения');
  expect(combined?.searchParams.get('neighborhood')).toBe('Левски');
});

test('successful empty search displays an empty state instead of a loading or error state', async ({ page }) => {
  await mockHomepage(page);
  await page.route('**/api/v1/locations?*', route => route.fulfill({
    json: { items: [], page: 1, pageSize: 12, total: 0 }
  }));

  await page.goto('/');
  await page.locator('input[name="q"]').fill('Няма такъв обект');
  await expect(page.locator('.empty-state')).toContainText('Няма резултати за тези филтри');
  await expect(page.locator('.error-state')).toHaveCount(0);
  await expect(page.locator('.skeleton-grid')).toHaveCount(0);
});

test('directory search failure exposes a retry action and preserves the query', async ({ page }) => {
  let attempts = 0;
  const requests: URL[] = [];
  await mockHomepage(page);
  await page.route('**/api/v1/locations?*', route => {
    attempts++;
    const url = new URL(route.request().url());
    requests.push(url);
    if (attempts === 1) return route.fulfill({ status: 503, body: 'Service Unavailable' });
    return route.fulfill({
      json: { items: [locations[1]], page: 1, pageSize: 12, total: 1 }
    });
  });

  await page.goto('/');
  await page.locator('input[name="q"]').fill('Кафене');
  await expect(page.locator('.error-state')).toContainText('Не успяхме да заредим обектите');
  await expect(page.locator('.error-state').getByRole('button', { name: 'Опитай отново' })).toBeEnabled();

  await page.locator('.error-state').getByRole('button', { name: 'Опитай отново' }).click();
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(1);
  await expect(page.locator('input[name="q"]')).toHaveValue('Кафене');
  expect(attempts).toBe(2);
  expect(requests.every(url => url.searchParams.get('q') === 'Кафене')).toBe(true);
});

test('a late directory response cannot replace results for a newer query', async ({ page }) => {
  let slowStarted = false;
  let slowFinished = false;
  await mockHomepage(page);

  await page.route('**/api/v1/locations?*', async route => {
    const url = new URL(route.request().url());
    const query = url.searchParams.get('q') ?? '';
    if (query === 'Кафе') {
      slowStarted = true;
      try {
        await new Promise(resolve => setTimeout(resolve, 250));
        await route.fulfill({
          json: { items: [locations[1]], page: 1, pageSize: 12, total: 1 }
        });
      } catch {
        // The active search unsubscribes the previous HttpClient request.
      } finally {
        slowFinished = true;
      }
      return;
    }

    const items = query === 'Вело' ? [locations[0]] : locations;
    return route.fulfill({
      json: { items, page: 1, pageSize: 12, total: items.length }
    });
  });

  await page.goto('/');
  await page.locator('input[name="q"]').fill('Кафе');
  await expect.poll(() => slowStarted).toBe(true);
  await page.locator('input[name="q"]').fill('Вело');

  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(1);
  await expect(page.locator('.location-card')).toContainText('Вело сервиз „Педал“');
  await expect.poll(() => slowFinished).toBe(true);
  await expect(page.locator('.location-card')).not.toContainText('Кафене „Ъгълът“');
});

test('directory pagination requests the selected page and updates the result list', async ({ page }) => {
  const firstPage = Array.from({ length: 12 }, (_, index) =>
    location(index + 1, `Обект ${index + 1}`, 'Услуги', 'Левски')
  );
  const secondPage = [location(13, 'Обект 13', 'Услуги', 'Левски')];
  let requestedPage = '';

  await page.route('**/api/v1/homepage', route => route.fulfill({
    json: {
      ...homepage,
      directory: { items: firstPage, page: 1, pageSize: 12, total: 13 }
    }
  }));
  await page.route('**/api/v1/locations?*', route => {
    const url = new URL(route.request().url());
    requestedPage = url.searchParams.get('page') ?? '';
    const items = requestedPage === '2' ? secondPage : firstPage;
    return route.fulfill({
      json: { items, page: Number(requestedPage), pageSize: 12, total: 13 }
    });
  });

  await page.goto('/');
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(12);
  await page.getByRole('button', { name: /Следваща/ }).click();
  await expect(page.locator('.location-card:not(.skeleton-card)')).toHaveCount(1);
  await expect(page.locator('.location-card')).toContainText('Обект 13');
  expect(requestedPage).toBe('2');
  await expect(page.locator('.pagination')).toContainText('Страница 2 от 2');
});

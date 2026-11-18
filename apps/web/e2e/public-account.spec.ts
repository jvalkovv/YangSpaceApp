import { expect, test } from '@playwright/test';

test('account routes render labelled forms', async ({ page }) => {
  for (const [route, heading] of [
    ['/login', 'Вход'],
    ['/register', 'Регистрация'],
    ['/recover', 'Забравена парола'],
    ['/reset', 'Нова парола'],
    ['/resend-confirmation', 'Потвърждение на имейл']
  ]) {
    await page.goto(route);
    await expect(page.getByRole('heading', { name: heading })).toBeVisible();
    await expect(page.getByLabel('Имейл адрес')).toBeVisible();
  }
});

test('registration remains disabled when the API default-off route returns 404', async ({ page }) => {
  await page.route('**/api/public-account/register', route =>
    route.fulfill({ status: 404, body: 'Not Found' })
  );
  await page.goto('/register');
  await page.getByLabel('Имейл адрес').fill('qa@example.invalid');
  await page.getByLabel('Парола').fill('Synthetic-Passphrase-For-Tests');
  await page.getByLabel('Повтори паролата').fill('Synthetic-Passphrase-For-Tests');
  await page.getByRole('button', { name: 'Създай акаунт' }).click();
  await expect(page.getByRole('alert')).toContainText('Регистрацията и входът още не са активни');
  await expect(page.locator('.auth-complete')).toHaveCount(0);
});

test('confirmation callback distinguishes valid and invalid links accessibly', async ({ page }) => {
  await page.goto('/login?confirmed=1');
  await expect(page.getByRole('status')).toContainText('Имейлът е потвърден');
  await page.goto('/login?confirmed=0');
  await expect(page.getByRole('alert')).toContainText('Линкът е невалиден или е изтекъл');
});

for (const width of [320, 360, 375, 390, 430, 768, 820, 1024, 1440]) {
  test(`account registration fits ${width}px without horizontal overflow`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/register');
    await expect(page.getByRole('heading', { name: 'Регистрация' })).toBeVisible();
    await expect(page.getByLabel('Имейл адрес')).toBeVisible();
    const metrics = await page.evaluate(() => ({
      viewport: window.innerWidth,
      document: document.documentElement.scrollWidth,
      body: document.body.scrollWidth
    }));
    expect(metrics.document).toBeLessThanOrEqual(width + 1);
    expect(metrics.body).toBeLessThanOrEqual(width + 1);
    await page.screenshot({
      path: testInfo.outputPath(`yangspace-account-register-${width}.png`),
      fullPage: true,
      animations: 'disabled'
    });
  });
}

test('account inputs have labels and adjacent controls are keyboard reachable', async ({ page }) => {
  await page.goto('/register');
  const email = page.getByLabel('Имейл адрес');
  const password = page.getByLabel('Парола');
  const repeat = page.getByLabel('Повтори паролата');
  await email.focus();
  await page.keyboard.press('Tab');
  await expect(password).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(repeat).toBeFocused();
  await page.keyboard.press('Shift+Tab');
  await expect(password).toBeFocused();
});

test('login uses the bearer header, keeps session in memory, and logout clears the profile', async ({ page }) => {
  let authorization = '';
  await page.route('**/api/public-account/login', route => route.fulfill({
    json: {
      tokenType: 'Bearer',
      accessToken: 'fixture-value',
      expiresIn: 3600,
      refreshToken: 'unused-fixture'
    }
  }));
  await page.route('**/api/public-account/session', async route => {
    authorization = route.request().headers().authorization ?? '';
    await route.fulfill({ json: { email: 'qa@example.invalid' } });
  });

  await page.goto('/login');
  await page.getByLabel('Имейл адрес').fill('qa@example.invalid');
  await page.getByLabel('Парола').fill('Synthetic-Passphrase-For-Tests');
  await page.getByRole('button', { name: 'Вход' }).click();

  await expect(page.locator('.auth-complete')).toContainText('Успешен вход');
  expect(authorization).toBe('Bearer fixture-value');
  const storage = await page.evaluate(() => ({
    local: localStorage.length,
    session: sessionStorage.length,
    cookie: document.cookie
  }));
  expect(storage).toEqual({ local: 0, session: 0, cookie: '' });

  await page.locator('.auth-complete').getByRole('link', { name: 'Моят профил' }).click();
  await expect(page.locator('.auth-profile')).toContainText('qa@example.invalid');
  await page.getByRole('button', { name: 'Изход' }).click();
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('heading', { name: 'Вход' })).toBeVisible();
});

test('failed login and rate limiting produce accessible generic errors', async ({ page }) => {
  let status = 401;
  await page.route('**/api/public-account/login', route =>
    route.fulfill({ status, body: JSON.stringify({ message: 'internal backend detail' }) })
  );
  await page.goto('/login');
  await page.getByLabel('Имейл адрес').fill('qa@example.invalid');
  await page.getByLabel('Парола').fill('Synthetic-Passphrase-For-Tests');
  await page.getByRole('button', { name: 'Вход' }).click();
  await expect(page.getByRole('alert')).toContainText('Неуспешен вход');
  await expect(page.getByRole('alert')).not.toContainText('internal backend detail');

  status = 429;
  await page.getByRole('button', { name: 'Вход' }).click();
  await expect(page.getByRole('alert')).toContainText('Твърде много опити');
  await expect(page.getByRole('alert')).toHaveAttribute('aria-live', 'assertive');
});

test('memory-only account session is not restored after a hard reload', async ({ page }) => {
  await page.route('**/api/public-account/login', route => route.fulfill({
    json: { tokenType: 'Bearer', accessToken: 'fixture-value', expiresIn: 3600, refreshToken: 'unused-fixture' }
  }));
  await page.route('**/api/public-account/session', route =>
    route.fulfill({ json: { email: 'qa@example.invalid' } })
  );
  await page.goto('/login');
  await page.getByLabel('Имейл адрес').fill('qa@example.invalid');
  await page.getByLabel('Парола').fill('Synthetic-Passphrase-For-Tests');
  await page.getByRole('button', { name: 'Вход' }).click();
  await expect(page.locator('.auth-complete')).toContainText('Успешен вход');

  await page.goto('/account');
  await expect(page.locator('.auth-profile')).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Вход в акаунта' })).toBeVisible();
  await expect(page.getByText(/След презареждане влез отново/)).toBeVisible();
  const storage = await page.evaluate(() => ({
    local: localStorage.length,
    session: sessionStorage.length,
    cookie: document.cookie
  }));
  expect(storage).toEqual({ local: 0, session: 0, cookie: '' });
});

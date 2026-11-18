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

import { defineConfig, devices } from '@playwright/test';

const previewPort = Number(process.env['H06_3_PREVIEW_PORT'] ?? '4300');
if (!Number.isInteger(previewPort) || previewPort < 1024 || previewPort > 65535) {
  throw new Error('H06_3_PREVIEW_PORT_INVALID');
}
const previewBaseUrl = `http://127.0.0.1:${previewPort}`;

export default defineConfig({
  testDir: './e2e',
  timeout: 20_000,
  expect: { timeout: 5_000 },
  fullyParallel: false,
  workers: 1,
  reporter: 'list',
  use: {
    baseURL: previewBaseUrl,
    ...devices['Desktop Chrome'],
    trace: 'retain-on-failure'
  },
  webServer: {
    command: `npm start -- --port ${previewPort}`,
    url: previewBaseUrl,
    reuseExistingServer: process.env['H06_3_REUSE_SERVER'] === '1',
    timeout: 90_000
  }
});

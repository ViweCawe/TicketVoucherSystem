const { defineConfig } = require('@playwright/test');

module.exports = defineConfig({
  testDir: '.',
  outputDir: 'test-results',
  reporter: 'line',
  use: {
    baseURL: process.env.BASE_URL,
    trace: 'retain-on-failure'
  }
});

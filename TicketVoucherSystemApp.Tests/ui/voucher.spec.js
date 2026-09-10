const { test, expect } = require('@playwright/test');

async function signIn(page) {
  await page.goto('/Identity/Account/Login');
  await page.getByLabel('Email').fill(process.env.ADMIN_EMAIL);
  await page.getByLabel('Password').fill(process.env.ADMIN_PASSWORD);
  await page.getByRole('button', { name: /log in/i }).click();
  await page.goto('/Operations/Issue');
  await expect(page.getByRole('heading', { name: 'Issue vouchers' })).toBeVisible();
}

for (const viewport of [
  { name: 'desktop', width: 1440, height: 960 },
  { name: 'mobile', width: 390, height: 844 }
]) {
  test(`issue screen renders on ${viewport.name}`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await signIn(page);
    const hasHorizontalOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth
    );
    expect(hasHorizontalOverflow).toBe(false);
    await page.screenshot({
      path: `artifacts/issue-${viewport.name}.png`,
      fullPage: true
    });

    await page.locator('.package-option input').first().check();
    await page.getByRole('button', { name: 'Issue vouchers' }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    await page.getByRole('button', { name: 'Close' }).click();
    await page.locator('.voucher-row').first().click();
    await expect(page.getByRole('heading', { name: 'Status history' })).toBeVisible();

    const downloadPromise = page.waitForEvent('download');
    await page.getByRole('link', { name: 'Download barcode' }).click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toMatch(/^voucher-.+\.svg$/);

    await page.screenshot({
      path: `artifacts/details-${viewport.name}.png`,
      fullPage: true
    });
  });
}

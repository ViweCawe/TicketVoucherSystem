const { test, expect } = require('@playwright/test');

async function signIn(page) {
  await page.goto('/Identity/Account/Login');
  await page.getByLabel('Email').fill(process.env.ADMIN_EMAIL);
  await page.getByLabel('Password').fill(process.env.ADMIN_PASSWORD);
  await page.getByRole('button', { name: /log in/i }).click();
  await page.goto('/Dashboard');
  await expect(page.getByRole('heading', { name: 'Voucher performance' })).toBeVisible();
}

for (const viewport of [
  { name: 'desktop', width: 1440, height: 960, suffix: 'D' },
  { name: 'mobile', width: 390, height: 844, suffix: 'M' }
]) {
  test(`combined package and group issuance work on ${viewport.name}`, async ({ page }) => {
    const marker = `${Date.now()}-${viewport.suffix}`;
    const packageName = `Group Combo ${marker}`;
    const ticketNumbers = [`GRP-${marker}-1`, `GRP-${marker}-2`];
    await page.setViewportSize(viewport);
    await signIn(page);

    await page.goto('/Admin/Packages');
    await page.getByLabel('Package name').fill(packageName);
    await page.getByLabel('Description').fill('Retail and food group package');
    await page.getByLabel('Retail value').fill('100');
    await page.getByLabel('F&B value').fill('200');
    await page.getByLabel('Valid for').fill('30');
    await page.getByRole('button', { name: 'Create package' }).click();
    await expect(page.getByText('Package created.')).toBeVisible();
    await expect(page.getByText(packageName, { exact: true })).toBeVisible();
    await page.screenshot({ path: `artifacts/packages-${viewport.name}.png`, fullPage: true });

    await page.goto('/Operations/Issue');
    const packageOption = page.getByLabel('Ticket package').locator('option').filter({ hasText: packageName });
    await page.getByLabel('Ticket package').selectOption(await packageOption.getAttribute('value'));
    await page.getByLabel('Existing ticket barcodes').fill(ticketNumbers.join('\n'));
    await expect(page.getByText('2 tickets', { exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Issue package to tickets' }).click();

    await expect(page.getByRole('button', { name: 'Print all 2 tickets' })).toBeVisible();
    await expect(page.locator('.batch-ticket')).toHaveCount(2);
    await expect(page.getByText(ticketNumbers[0], { exact: true })).toBeVisible();
    await expect(page.getByText(ticketNumbers[1], { exact: true })).toBeVisible();
    await expect(page.getByText('R100 OFF', { exact: true }).first()).toBeVisible();
    await expect(page.getByText('R200 OFF', { exact: true }).first()).toBeVisible();
    await page.screenshot({ path: `artifacts/group-print-${viewport.name}.png`, fullPage: true });

    await expect(page.locator('a[href*="/Operations/Redeem"]')).toHaveCount(0);
    const removedPage = await page.request.get('/Operations/Redeem');
    expect(removedPage.status()).toBe(404);
    const removedApi = await page.request.post('/api/pos/v1/vouchers/redeem', { data: {} });
    // Static-asset fallback routing can reject an unmatched POST with 405.
    expect([404, 405]).toContain(removedApi.status());

    // The print view intentionally uses A4 width; check a normal app page on mobile.
    await page.goto('/Dashboard');
    const hasHorizontalOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth
    );
    expect(hasHorizontalOverflow).toBe(false);
  });
}


const { test, expect } = require('@playwright/test');

async function signIn(page) {
  await page.goto('/Identity/Account/Login');
  await page.getByLabel('Email').fill(process.env.ADMIN_EMAIL);
  await page.getByLabel('Password').fill(process.env.ADMIN_PASSWORD);
  await page.getByRole('button', { name: /log in/i }).click();
  await page.goto('/Dashboard');
  await expect(page.getByRole('heading', { name: 'Voucher performance' })).toBeVisible();
}

test('identity screen has the dedicated secure layout', async ({ page }) => {
  await page.goto('/Identity/Account/Login');
  await expect(page.getByText('CONTROLLED ACCESS')).toBeVisible();
  await expect(page.getByRole('heading', { name: /One account/i })).toBeVisible();
});

for (const viewport of [
  { name: 'desktop', width: 1440, height: 960, barcode: '9100000001' },
  { name: 'mobile', width: 390, height: 844, barcode: '9100000002' }
]) {
  test(`inventory, issue, redeem, and reports work on ${viewport.name}`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await signIn(page);

    await page.goto('/Operations/Import');
    await page.getByLabel('Ticket barcode list').fill(viewport.barcode);
    await page.getByRole('button', { name: 'Import barcode stock' }).click();
    await expect(page.getByText('1 imported')).toBeVisible();

    await page.goto('/Operations/Issue');
    await expect(page.getByRole('heading', { name: 'Build vouchers from available tickets' })).toBeVisible();
    await page.locator('.package-cell input').first().check();
    await page.getByRole('button', { name: 'Allocate and issue' }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    const issuedCode = await page.getByRole('dialog').locator('.issued-list strong').first().textContent();
    expect(issuedCode).toBe(viewport.barcode);

    const downloadPromise = page.waitForEvent('download');
    await page.getByRole('dialog').getByRole('link', { name: 'Download' }).click();
    expect((await downloadPromise).suggestedFilename()).toMatch(/^voucher-.+\.svg$/);
    await page.getByRole('button', { name: 'Close' }).click();

    await page.goto('/Operations/Redeem?department=Retail');
    await page.getByLabel('Redeeming outlet').selectOption({ label: 'Vista' });
    await page.getByPlaceholder('Scan or enter 10-digit barcode').fill(viewport.barcode);
    await page.getByRole('button', { name: 'Redeem voucher' }).click();
    await expect(page.getByText('REDEEMED')).toBeVisible();
    await expect(page.getByText(/at Vista/)).toBeVisible();
    await expect(page.getByLabel('Redeeming outlet')).toHaveValue(/\d+/);
    await expect(page.getByPlaceholder('Scan or enter 10-digit barcode')).toHaveValue('');

    await page.goto('/Reports');
    await expect(page.getByRole('heading', { name: 'Where voucher value moves' })).toBeVisible();
    await expect(page.getByText('Vista').first()).toBeVisible();

    const hasHorizontalOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth
    );
    expect(hasHorizontalOverflow).toBe(false);
    await page.screenshot({ path: `artifacts/report-${viewport.name}.png`, fullPage: true });
  });
}

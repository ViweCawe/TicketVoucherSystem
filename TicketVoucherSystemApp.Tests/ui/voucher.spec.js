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
  { name: 'desktop', width: 1440, height: 960, suffix: 'D' },
  { name: 'mobile', width: 390, height: 844, suffix: 'M' }
]) {
  test(`ticket package, print, redemption, and reports work on ${viewport.name}`, async ({ page }) => {
    const ticketNumber = `VIP-${Date.now()}-${viewport.suffix}`;
    await page.setViewportSize(viewport);
    await signIn(page);

    await page.goto('/Operations/Issue');
    await expect(page.getByRole('heading', { name: /Scan the ticket/i })).toBeVisible();
    await page.getByLabel('Existing ticket barcode').fill(ticketNumber);
    await page.getByText('VIP Ticket', { exact: true }).click();
    await page.getByRole('button', { name: 'Attach package and continue' }).click();

    await expect(page.getByText(ticketNumber, { exact: true })).toBeVisible();
    await expect(page.getByText('F&B R100', { exact: true })).toBeVisible();
    await expect(page.getByText('Retail R250', { exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Print ticket' })).toBeVisible();
    await expect(page.locator('.print-barcode img')).toBeVisible();

    const downloadPromise = page.waitForEvent('download');
    await page.getByRole('link', { name: 'Download barcode' }).click();
    expect((await downloadPromise).suggestedFilename()).toBe(`ticket-${ticketNumber}.svg`);

    await page.goto('/Operations/Redeem?department=Retail');
    await page.getByLabel('Redeeming outlet').selectOption({ label: 'Vista' });
    await page.getByPlaceholder(/Scan or enter/i).fill(ticketNumber);
    await page.getByRole('button', { name: 'Redeem voucher' }).click();
    await expect(page.getByText('REDEEMED')).toBeVisible();
    await expect(page.getByText(/at Vista/)).toBeVisible();

    await page.goto('/Reports');
    await expect(page.getByRole('heading', { name: 'Where voucher value moves' })).toBeVisible();
    await expect(page.getByText('Vista').first()).toBeVisible();

    const hasHorizontalOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth
    );
    expect(hasHorizontalOverflow).toBe(false);
  });
}

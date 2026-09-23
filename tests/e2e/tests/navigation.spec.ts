import { expect, test } from '@playwright/test'

for (const [link, heading] of [
  ['Invoices', 'Invoices'],
  ['Payments', 'Payments'],
  ['Clients', 'Clients'],
] as const) {
  test(`${link} link opens the ${heading} page`, async ({ page }) => {
    await page.goto('/')
    await page.getByRole('navigation', { name: 'Main' }).getByRole('link', { name: link }).click()

    await expect(page.getByRole('heading', { level: 1, name: heading })).toBeVisible()
  })
}

import { expect, test } from '@playwright/test'

test('adds a client and lists it', async ({ page }) => {
  const companyName = `E2E Client ${Date.now()}`

  await page.goto('/#/clients')
  await page.getByRole('button', { name: 'Add client' }).click()

  await page.getByLabel('Company name').fill(companyName)
  await page.getByLabel('Email').fill('billing@e2e.example')
  await page.getByLabel('Address line 1').fill('1 Test Street')
  await page.getByLabel('City').fill('Sydney')
  await page.getByLabel('State / region').fill('NSW')
  await page.getByLabel('Postal code').fill('2000')
  await page.getByLabel('Country').fill('Australia')
  await page.getByLabel('Preferred currency').fill('AUD')
  await page.getByRole('button', { name: 'Add client' }).click()

  await expect(page.getByRole('heading', { level: 1, name: 'Clients' })).toBeVisible()
  await expect(page.getByRole('cell', { name: companyName, exact: true })).toBeVisible()
})

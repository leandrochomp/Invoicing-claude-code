import { expect, test } from '@playwright/test'
import { createSentInvoice } from './helpers'

test('records a full payment from the invoice', async ({ page }) => {
  const companyName = await createSentInvoice(page)

  await page.getByRole('button', { name: 'Record payment' }).click()
  // The amount starts at the balance due.
  await expect(page.getByLabel('Amount (AUD)')).toHaveValue('150.00')
  await page.getByRole('button', { name: 'Record payment' }).click()

  await expect(page.getByRole('button', { name: /^Delete payment of .*150\.00$/ })).toBeVisible()
  // Nothing is left to collect.
  await expect(page.getByRole('button', { name: 'Record payment' })).toBeHidden()

  await page.goto('/#/payments')
  await expect(page.getByRole('cell', { name: companyName, exact: true })).toBeVisible()
})

test('records a part payment from the payments page', async ({ page }) => {
  const companyName = await createSentInvoice(page)

  await page.goto('/#/payments')
  await page.getByRole('link', { name: 'Record payment' }).click()
  const invoice = page.getByLabel('Invoice')
  const value = await invoice.locator('option', { hasText: companyName }).getAttribute('value')
  await invoice.selectOption(value!)
  await expect(page.getByLabel('Amount (AUD)')).toHaveValue('150.00')
  await page.getByLabel('Amount (AUD)').fill('50')
  await page.getByRole('button', { name: 'Record payment' }).click()

  await expect(page.getByRole('heading', { level: 1, name: 'Payments' })).toBeVisible()
  const row = page.getByRole('row').filter({ hasText: companyName })
  await expect(row).toContainText('50.00')

  // The invoice stays open for the rest.
  await row.getByRole('link').click()
  await expect(page.getByText('Sent', { exact: true })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Record payment' })).toBeVisible()
})

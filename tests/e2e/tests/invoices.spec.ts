import { expect, test } from '@playwright/test'
import { addClient, createDraftInvoice, uniqueName } from './helpers'

test('creates a draft invoice and marks it as sent', async ({ page }) => {
  const companyName = uniqueName('E2E Invoice Client')
  const description = uniqueName('Consulting')
  await addClient(page, companyName)

  await createDraftInvoice(page, companyName, description)

  await expect(page.getByText(companyName)).toBeVisible()
  await expect(page.getByRole('cell', { name: description, exact: true })).toBeVisible()
  await expect(page.getByText('Draft', { exact: true })).toBeVisible()

  await page.getByRole('button', { name: 'Mark as sent' }).click()

  await expect(page.getByText('Sent', { exact: true })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Record payment' })).toBeVisible()
})

test('lists a new invoice and opens it from the list', async ({ page }) => {
  const companyName = uniqueName('E2E Invoice Client')
  const description = uniqueName('Consulting')
  await addClient(page, companyName)
  await createDraftInvoice(page, companyName, description)
  const heading = await page.getByRole('heading', { level: 1, name: /^Invoice / }).textContent()
  const number = heading!.replace(/^Invoice /, '')

  await page.getByRole('button', { name: 'Back to invoices' }).click()
  await page.getByRole('button', { name: 'Draft', exact: true }).click()
  await page.getByRole('link', { name: number, exact: true }).click()

  await expect(page.getByRole('cell', { name: description, exact: true })).toBeVisible()
})

import { expect, type Page } from '@playwright/test'

// Specs write to the dev database, so every client gets a unique name.
export function uniqueName(prefix: string) {
  return `${prefix} ${Date.now()}-${Math.random().toString(36).slice(2, 6)}`
}

export async function addClient(page: Page, companyName: string) {
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

  await expect(page.getByRole('cell', { name: companyName, exact: true })).toBeVisible()
}

// Creates a one-line draft for the client and leaves the page on the new invoice's detail view.
export async function createDraftInvoice(page: Page, companyName: string, description: string) {
  await page.goto('/#/invoices/new')
  await page.getByRole('combobox', { name: 'Client' }).selectOption({ label: companyName })
  await page.getByLabel('Currency').fill('AUD')

  const line = page.getByRole('group', { name: 'Line 1' })
  await line.getByLabel('Description').fill(description)
  await line.getByLabel('Unit price').fill('150')
  await page.getByRole('button', { name: 'Create draft' }).click()

  await expect(page.getByRole('heading', { level: 1, name: /^Invoice / })).toBeVisible()
}

// Adds a client with a sent A$150 invoice, leaves the page on that invoice and returns the client's name.
export async function createSentInvoice(page: Page) {
  const companyName = uniqueName('E2E Payment Client')
  await addClient(page, companyName)
  await createDraftInvoice(page, companyName, 'Consulting')
  await page.getByRole('button', { name: 'Mark as sent' }).click()
  await expect(page.getByRole('button', { name: 'Record payment' })).toBeVisible()
  return companyName
}

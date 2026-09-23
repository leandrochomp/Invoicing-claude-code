import { expect, test } from '@playwright/test'
import { addClient, uniqueName } from './helpers'

test('adds a client and lists it', async ({ page }) => {
  const companyName = uniqueName('E2E Client')

  await addClient(page, companyName)

  await expect(page.getByRole('heading', { level: 1, name: 'Clients' })).toBeVisible()
})

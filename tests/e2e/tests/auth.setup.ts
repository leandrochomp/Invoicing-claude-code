import { expect, test as setup } from '@playwright/test'

const username = process.env.E2E_USERNAME
const password = process.env.E2E_PASSWORD

setup('sign in', async ({ page }) => {
  if (!username || !password) {
    throw new Error('Set E2E_USERNAME and E2E_PASSWORD to a user in the local dev database.')
  }

  await page.goto('/')
  await page.getByRole('textbox', { name: 'Username' }).fill(username)
  await page.getByRole('textbox', { name: 'Password' }).fill(password)
  await page.getByRole('button', { name: 'Sign in' }).click()

  await expect(page.getByRole('heading', { name: `Welcome back, ${username}` })).toBeVisible()

  // Invoicing data is per tenant. A user without one (like the seeded Admin) signs in fine but is refused
  // every data endpoint, so stop here instead of letting each data spec time out.
  const clients = await page.request.get('/bff/clients')
  expect(
    clients.ok(),
    `${username} can't read invoicing data (HTTP ${clients.status()}). Use a tenant user, such as the seeded Owner.`,
  ).toBe(true)

  await page.context().storageState({ path: '.auth/user.json' })
})

import { expect, test } from '@playwright/test'

test.describe('signed out', () => {
  test.use({ storageState: { cookies: [], origins: [] } })

  test('shows the sign-in form', async ({ page }) => {
    await page.goto('/')

    await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible()
  })

  test('rejects a wrong password', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('textbox', { name: 'Username' }).fill('no-such-user')
    await page.getByRole('textbox', { name: 'Password' }).fill('wrong-password')
    await page.getByRole('button', { name: 'Sign in' }).click()

    await expect(page.getByRole('alert')).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible()
  })
})

test('keeps the session across a reload', async ({ page }) => {
  await page.goto('/')
  await page.reload()

  await expect(page.getByRole('button', { name: 'Sign out' })).toBeVisible()
})

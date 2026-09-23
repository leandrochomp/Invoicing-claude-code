import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getSession } from './api/authApi'
import App from './App'

vi.mock('./api/authApi', () => ({ getSession: vi.fn(), logout: vi.fn() }))
vi.mock('./pages/HomePage', () => ({ HomePage: () => <h1>Home page</h1> }))
vi.mock('./pages/InvoicesPage', () => ({ InvoicesPage: ({ route }: { route: { view: string } }) => <h1>Invoices {route.view}</h1> }))
vi.mock('./pages/PaymentsPage', () => ({ PaymentsPage: () => <h1>Payments page</h1> }))
vi.mock('./pages/ClientsPage', () => ({ ClientsPage: () => <h1>Clients page</h1> }))

beforeEach(() => {
  window.location.hash = ''
  vi.mocked(getSession).mockResolvedValue({ username: 'alice' })
})

afterEach(() => {
  vi.clearAllMocks()
})

describe('App navigation', () => {
  it('starts on the home page', async () => {
    render(<App />)

    expect(await screen.findByRole('heading', { name: 'Home page' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Home' })).toHaveAttribute('aria-current', 'page')
  })

  it('follows the URL hash between pages', async () => {
    render(<App />)
    await screen.findByRole('heading', { name: 'Home page' })

    act(() => {
      window.location.hash = '#/invoices/abc'
      window.dispatchEvent(new HashChangeEvent('hashchange'))
    })

    expect(await screen.findByRole('heading', { name: 'Invoices detail' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Invoices' })).toHaveAttribute('aria-current', 'page')
  })

  it('opens the page named in the hash on load', async () => {
    window.location.hash = '#/payments'

    render(<App />)

    expect(await screen.findByRole('heading', { name: 'Payments page' })).toBeInTheDocument()
  })
})

describe('App sidebar', () => {
  it('shows the main navigation and signed-in user in the sidebar', async () => {
    render(<App />)
    await screen.findByRole('heading', { name: 'Home page' })

    const sidebar = screen.getByRole('complementary')
    expect(sidebar).toContainElement(screen.getByRole('navigation', { name: 'Main' }))
    expect(sidebar).toHaveTextContent('Signed in as alice')
  })

  it('toggles the menu open and closed', async () => {
    const user = userEvent.setup()
    render(<App />)
    await screen.findByRole('heading', { name: 'Home page' })
    const toggle = screen.getByRole('button', { name: 'Menu' })

    expect(toggle).toHaveAttribute('aria-expanded', 'false')
    await user.click(toggle)
    expect(toggle).toHaveAttribute('aria-expanded', 'true')
    expect(toggle).toHaveTextContent('Close')
    await user.click(toggle)
    expect(toggle).toHaveAttribute('aria-expanded', 'false')
  })

  it('closes the menu after navigating to another page', async () => {
    const user = userEvent.setup()
    render(<App />)
    await screen.findByRole('heading', { name: 'Home page' })
    await user.click(screen.getByRole('button', { name: 'Menu' }))

    act(() => {
      window.location.hash = '#/clients'
      window.dispatchEvent(new HashChangeEvent('hashchange'))
    })

    expect(await screen.findByRole('heading', { name: 'Clients page' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Menu' })).toHaveAttribute('aria-expanded', 'false')
  })
})

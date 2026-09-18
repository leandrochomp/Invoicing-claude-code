import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createClient, deleteClient, getClient, listClients, updateClient } from '../api/clientsApi'
import { ClientsPage } from './ClientsPage'

vi.mock('../api/clientsApi', () => ({
  listClients: vi.fn(),
  getClient: vi.fn(),
  createClient: vi.fn(),
  updateClient: vi.fn(),
  deleteClient: vi.fn(),
  ClientApiError: class ClientApiError extends Error {},
}))

const summary = { id: '1', companyName: 'Acme Corp', email: 'billing@acme.test' }

const detail = {
  id: '1',
  companyName: 'Acme Corp',
  contactName: 'Jane Doe',
  email: 'billing@acme.test',
  phone: '555-0100',
  addressLine1: '1 Main St',
  addressLine2: null,
  city: 'Springfield',
  stateOrRegion: 'IL',
  postalCode: '62701',
  country: 'US',
  preferredCurrency: 'USD',
  isActive: true,
}

beforeEach(() => {
  vi.mocked(listClients).mockResolvedValue([summary])
})

afterEach(() => {
  vi.clearAllMocks()
})

describe('ClientsPage', () => {
  it('renders the client list', async () => {
    render(<ClientsPage />)

    expect(await screen.findByText('Acme Corp')).toBeInTheDocument()
    expect(screen.getByText('billing@acme.test')).toBeInTheDocument()
  })

  it('creates a client and returns to the list', async () => {
    vi.mocked(createClient).mockResolvedValue(summary)
    const user = userEvent.setup()
    render(<ClientsPage />)
    await screen.findByText('Acme Corp')

    await user.click(screen.getByRole('button', { name: 'Add client' }))
    await user.type(screen.getByLabelText('Company name'), 'Globex')
    await user.type(screen.getByLabelText('Email'), 'billing@globex.test')
    await user.type(screen.getByLabelText('Address line 1'), '2 Main St')
    await user.type(screen.getByLabelText('City'), 'Shelbyville')
    await user.type(screen.getByLabelText('State / region'), 'IL')
    await user.type(screen.getByLabelText('Postal code'), '62702')
    await user.type(screen.getByLabelText('Country'), 'US')
    await user.type(screen.getByLabelText('Preferred currency'), 'USD')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(createClient).toHaveBeenCalledWith(
      expect.objectContaining({ companyName: 'Globex', email: 'billing@globex.test' }),
    ))
    expect(await screen.findByRole('heading', { name: 'Clients' })).toBeInTheDocument()
  })

  it('loads client details into the form and submits an update', async () => {
    vi.mocked(getClient).mockResolvedValue(detail)
    vi.mocked(updateClient).mockResolvedValue(summary)
    const user = userEvent.setup()
    render(<ClientsPage />)
    await screen.findByText('Acme Corp')

    await user.click(screen.getByRole('button', { name: 'Edit' }))

    expect(await screen.findByDisplayValue('Acme Corp')).toBeInTheDocument()
    expect(screen.getByLabelText('Active')).toBeChecked()

    await user.click(screen.getByLabelText('Active'))
    await user.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(updateClient).toHaveBeenCalledWith(
      '1',
      expect.objectContaining({ companyName: 'Acme Corp', isActive: false }),
    ))
  })

  it('deletes a client after confirmation and refreshes the list', async () => {
    vi.stubGlobal('confirm', vi.fn().mockReturnValue(true))
    vi.mocked(deleteClient).mockResolvedValue(undefined)
    const user = userEvent.setup()
    render(<ClientsPage />)
    await screen.findByText('Acme Corp')

    await user.click(screen.getByRole('button', { name: 'Delete' }))

    await waitFor(() => expect(deleteClient).toHaveBeenCalledWith('1'))
    expect(listClients).toHaveBeenCalledTimes(2)
  })

  it('does not delete when the confirmation is dismissed', async () => {
    vi.stubGlobal('confirm', vi.fn().mockReturnValue(false))
    const user = userEvent.setup()
    render(<ClientsPage />)
    await screen.findByText('Acme Corp')

    await user.click(screen.getByRole('button', { name: 'Delete' }))

    expect(deleteClient).not.toHaveBeenCalled()
  })

  it('shows an error message when the list fails to load', async () => {
    const { ClientApiError } = await import('../api/clientsApi')
    vi.mocked(listClients).mockRejectedValue(new ClientApiError('Unable to reach the Invoicing API.'))

    render(<ClientsPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Unable to reach the Invoicing API.')
  })

  it('cancels out of the create form back to the list', async () => {
    const user = userEvent.setup()
    render(<ClientsPage />)
    await screen.findByText('Acme Corp')

    await user.click(screen.getByRole('button', { name: 'Add client' }))
    expect(screen.getByRole('heading', { name: 'Add client' })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(await screen.findByRole('heading', { name: 'Clients' })).toBeInTheDocument()
  })
})

// Sanity check that table rows scope their actions (guards against accidental global button queries).
describe('ClientsPage row scoping', () => {
  it('renders one action cell per client row', async () => {
    render(<ClientsPage />)
    const row = await screen.findByText('Acme Corp')
    const tr = row.closest('tr')
    expect(tr).not.toBeNull()
    expect(within(tr as HTMLElement).getByRole('button', { name: 'Edit' })).toBeInTheDocument()
  })
})

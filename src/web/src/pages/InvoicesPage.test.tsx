import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getClient, listClients } from '../api/clientsApi'
import type { Invoice } from '../api/invoicesApi'
import { InvoiceStatus, createInvoice, getInvoice, listInvoices, updateInvoice } from '../api/invoicesApi'
import { InvoicesPage } from './InvoicesPage'

vi.mock('../api/clientsApi', () => ({
  listClients: vi.fn(),
  getClient: vi.fn(),
}))

vi.mock('../api/invoicesApi', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/invoicesApi')>()),
  listInvoices: vi.fn(),
  getInvoice: vi.fn(),
  createInvoice: vi.fn(),
  updateInvoice: vi.fn(),
  deleteInvoice: vi.fn(),
}))

const clients = [{ id: 'client-1', companyName: 'Acme Corp', email: 'billing@acme.test' }]

const summary = {
  id: 'inv-1',
  clientId: 'client-1',
  invoiceNumber: 1001,
  status: InvoiceStatus.Draft,
  issueDate: '2026-09-01T00:00:00+00:00',
  dueDate: '2099-10-01T00:00:00+00:00',
  currency: 'USD',
  grandTotal: 330,
}

const invoice: Invoice = {
  ...summary,
  subTotal: 300,
  taxTotal: 30,
  notes: 'Thanks!',
  version: 2,
  items: [{ id: 'item-1', description: 'Consulting', quantity: 2, unitPrice: 150, taxRate: 0.1, lineTotal: 300, sortOrder: 0 }],
  payments: [],
}

beforeEach(() => {
  window.location.hash = ''
  vi.mocked(listClients).mockResolvedValue(clients)
  vi.mocked(listInvoices).mockResolvedValue({ items: [summary], page: 1, pageSize: 25, totalRecords: 1, totalPages: 1 })
})

afterEach(() => {
  vi.clearAllMocks()
})

describe('InvoicesPage list', () => {
  it('shows each invoice with its client, total and status', async () => {
    render(<InvoicesPage route={{ page: 'invoices', view: 'list' }} />)

    const link = await screen.findByRole('link', { name: '#1001' })
    const row = link.closest('tr') as HTMLElement
    expect(link).toHaveAttribute('href', '#/invoices/inv-1')
    await waitFor(() => expect(within(row).getByText('Acme Corp')).toBeInTheDocument())
    expect(within(row).getByText('Draft')).toBeInTheDocument()
  })

  it('labels invoices whose client has since been deleted', async () => {
    vi.mocked(listInvoices).mockResolvedValue({
      items: [{ ...summary, clientId: 'gone' }],
      page: 1,
      pageSize: 25,
      totalRecords: 1,
      totalPages: 1,
    })

    render(<InvoicesPage route={{ page: 'invoices', view: 'list' }} />)

    expect(await screen.findByText('Deleted client')).toBeInTheDocument()
  })

  it('shows a sent invoice past its due date as overdue', async () => {
    vi.mocked(listInvoices).mockResolvedValue({
      items: [{ ...summary, status: InvoiceStatus.Sent, dueDate: '2020-01-01T00:00:00+00:00' }],
      page: 1,
      pageSize: 25,
      totalRecords: 1,
      totalPages: 1,
    })

    render(<InvoicesPage route={{ page: 'invoices', view: 'list' }} />)

    const row = (await screen.findByRole('link', { name: '#1001' })).closest('tr') as HTMLElement
    expect(within(row).getByText('Overdue')).toBeInTheDocument()
  })

  it('filters by status and starts again from the first page', async () => {
    const user = userEvent.setup()
    render(<InvoicesPage route={{ page: 'invoices', view: 'list' }} />)
    await screen.findByRole('link', { name: '#1001' })

    await user.click(screen.getByRole('button', { name: 'Paid' }))

    await waitFor(() =>
      expect(listInvoices).toHaveBeenLastCalledWith({ status: InvoiceStatus.Paid, page: 1, pageSize: 25 }),
    )
    expect(screen.getByRole('button', { name: 'Paid' })).toHaveAttribute('aria-pressed', 'true')
  })

  it('offers to create the first invoice when there are none', async () => {
    vi.mocked(listInvoices).mockResolvedValue({ items: [], page: 1, pageSize: 25, totalRecords: 0, totalPages: 0 })

    render(<InvoicesPage route={{ page: 'invoices', view: 'list' }} />)

    expect(await screen.findByRole('heading', { name: 'No invoices yet' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Create your first invoice' })).toHaveAttribute('href', '#/invoices/new')
  })
})

describe('InvoicesPage create', () => {
  it('creates a draft with the tax rate as a fraction and opens it', async () => {
    vi.mocked(getClient).mockResolvedValue({ preferredCurrency: 'USD' } as Awaited<ReturnType<typeof getClient>>)
    vi.mocked(createInvoice).mockResolvedValue({ ...invoice, id: 'inv-new' })
    const user = userEvent.setup()
    render(<InvoicesPage route={{ page: 'invoices', view: 'new' }} />)

    await user.selectOptions(await screen.findByLabelText('Client *'), 'client-1')
    await waitFor(() => expect(screen.getByLabelText('Currency *')).toHaveValue('USD'))
    await user.clear(screen.getByLabelText('Issue date *'))
    await user.type(screen.getByLabelText('Issue date *'), '01/09/2026')
    await user.clear(screen.getByLabelText('Due date *'))
    await user.type(screen.getByLabelText('Due date *'), '01/10/2026')
    await user.type(screen.getByLabelText('Description (Line 1) *'), 'Consulting')
    await user.clear(screen.getByLabelText('Quantity (Line 1)'))
    await user.type(screen.getByLabelText('Quantity (Line 1)'), '2')
    await user.type(screen.getByLabelText('Unit price (Line 1) *'), '150')
    await user.clear(screen.getByLabelText('Tax % (Line 1)'))
    await user.type(screen.getByLabelText('Tax % (Line 1)'), '10')
    await user.click(screen.getByRole('button', { name: 'Create draft' }))

    await waitFor(() =>
      expect(createInvoice).toHaveBeenCalledWith({
        clientId: 'client-1',
        issueDate: '2026-09-01T00:00:00Z',
        dueDate: '2026-10-01T00:00:00Z',
        currency: 'USD',
        notes: null,
        items: [{ id: null, description: 'Consulting', quantity: 2, unitPrice: 150, taxRate: 0.1, sortOrder: 0 }],
      }),
    )
    expect(window.location.hash).toBe('#/invoices/inv-new')
  })

  it('shows the server’s reason when the invoice can’t be created', async () => {
    const { ApiError } = await import('../api/http')
    vi.mocked(createInvoice).mockRejectedValue(new ApiError("Client 'client-1' does not exist.", 400))
    const user = userEvent.setup()
    render(<InvoicesPage route={{ page: 'invoices', view: 'new' }} />)

    await user.selectOptions(await screen.findByLabelText('Client *'), 'client-1')
    await user.type(screen.getByLabelText('Currency *'), 'USD')
    await user.type(screen.getByLabelText('Description (Line 1) *'), 'Consulting')
    await user.type(screen.getByLabelText('Unit price (Line 1) *'), '150')
    await user.click(screen.getByRole('button', { name: 'Create draft' }))

    expect(await screen.findByRole('alert')).toHaveTextContent("Client 'client-1' does not exist.")
  })

  it('asks for a client first when there are none', async () => {
    vi.mocked(listClients).mockResolvedValue([])

    render(<InvoicesPage route={{ page: 'invoices', view: 'new' }} />)

    expect(await screen.findByRole('heading', { name: 'Add a client first' })).toBeInTheDocument()
  })
})

describe('InvoicesPage edit', () => {
  it('loads the invoice into the form and saves it with its version and line ids', async () => {
    vi.mocked(getInvoice).mockResolvedValue(invoice)
    vi.mocked(updateInvoice).mockResolvedValue(invoice)
    const user = userEvent.setup()
    render(<InvoicesPage route={{ page: 'invoices', view: 'edit', id: 'inv-1' }} />)

    const description = await screen.findByLabelText('Description (Line 1) *')
    expect(description).toHaveValue('Consulting')
    expect(screen.getByLabelText('Tax % (Line 1)')).toHaveValue(10)

    await user.clear(description)
    await user.type(description, 'Strategy consulting')
    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    await waitFor(() =>
      expect(updateInvoice).toHaveBeenCalledWith(
        'inv-1',
        expect.objectContaining({
          status: InvoiceStatus.Draft,
          version: 2,
          notes: 'Thanks!',
          items: [{ id: 'item-1', description: 'Strategy consulting', quantity: 2, unitPrice: 150, taxRate: 0.1, sortOrder: 0 }],
        }),
      ),
    )
    expect(window.location.hash).toBe('#/invoices/inv-1')
  })
})

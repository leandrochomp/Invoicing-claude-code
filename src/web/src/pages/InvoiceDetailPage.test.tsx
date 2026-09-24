import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Invoice } from '../api/invoicesApi'
import { InvoiceStatus, deleteInvoice, getInvoice, sendInvoice, voidInvoice } from '../api/invoicesApi'
import { PaymentMethod, createPayment, deletePayment } from '../api/paymentsApi'
import { InvoiceDetailPage } from './InvoiceDetailPage'

vi.mock('../api/invoicesApi', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/invoicesApi')>()),
  getInvoice: vi.fn(),
  deleteInvoice: vi.fn(),
  sendInvoice: vi.fn(),
  voidInvoice: vi.fn(),
}))

vi.mock('../api/paymentsApi', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/paymentsApi')>()),
  createPayment: vi.fn(),
  updatePayment: vi.fn(),
  deletePayment: vi.fn(),
}))

const clients = [{ id: 'client-1', companyName: 'Acme Corp', email: 'billing@acme.test' }]

const draft: Invoice = {
  id: 'inv-1',
  clientId: 'client-1',
  invoiceNumber: 1001,
  status: InvoiceStatus.Draft,
  issueDate: '2026-09-01T00:00:00+00:00',
  dueDate: '2099-10-01T00:00:00+00:00',
  currency: 'USD',
  subTotal: 300,
  taxTotal: 30,
  grandTotal: 330,
  notes: null,
  version: 2,
  items: [{ id: 'item-1', description: 'Consulting', quantity: 2, unitPrice: 150, taxRate: 0.1, lineTotal: 300, sortOrder: 0 }],
  payments: [],
}

const partPaid: Invoice = {
  ...draft,
  status: InvoiceStatus.Sent,
  payments: [
    { id: 'pay-1', invoiceId: 'inv-1', amount: 100, paymentDate: '2026-09-05T00:00:00+00:00', method: PaymentMethod.Card, notes: null, version: 0 },
  ],
}

beforeEach(() => {
  window.location.hash = '#/invoices/inv-1'
})

afterEach(() => {
  vi.clearAllMocks()
  vi.unstubAllGlobals()
})

describe('InvoiceDetailPage', () => {
  it('shows the invoice, its client and its line items', async () => {
    vi.mocked(getInvoice).mockResolvedValue(draft)

    render(<InvoiceDetailPage id="inv-1" clients={clients} />)

    expect(await screen.findByRole('heading', { name: 'Invoice #1001' })).toBeInTheDocument()
    expect(screen.getByText(/Acme Corp/)).toBeInTheDocument()
    expect(screen.getByText('Consulting')).toBeInTheDocument()
    expect(screen.getByText('Mark this invoice as sent to start recording payments.')).toBeInTheDocument()
  })

  it('marks a draft as sent, sending the version it was loaded with', async () => {
    vi.mocked(getInvoice).mockResolvedValue(draft)
    vi.mocked(sendInvoice).mockResolvedValue({ ...draft, status: InvoiceStatus.Sent, version: 3 })
    const user = userEvent.setup()
    render(<InvoiceDetailPage id="inv-1" clients={clients} />)

    await user.click(await screen.findByRole('button', { name: 'Mark as sent' }))

    expect(sendInvoice).toHaveBeenCalledWith('inv-1', 2)
    expect(await screen.findByRole('button', { name: 'Record payment' })).toBeInTheDocument()
  })

  it('records a payment, starting from the balance still owed', async () => {
    vi.mocked(getInvoice).mockResolvedValueOnce(partPaid).mockResolvedValueOnce({ ...partPaid, status: InvoiceStatus.Paid })
    vi.mocked(createPayment).mockResolvedValue(partPaid.payments[0])
    const user = userEvent.setup()
    render(<InvoiceDetailPage id="inv-1" clients={clients} />)

    await user.click(await screen.findByRole('button', { name: 'Record payment' }))
    expect(screen.getByLabelText('Amount (USD) *')).toHaveValue(230)
    await user.selectOptions(screen.getByLabelText('Method *'), 'Cash')
    await user.click(screen.getByRole('button', { name: 'Record payment' }))

    await waitFor(() =>
      expect(createPayment).toHaveBeenCalledWith('inv-1', expect.objectContaining({ amount: 230, method: PaymentMethod.Cash })),
    )
    expect(getInvoice).toHaveBeenCalledTimes(2)
  })

  it('won’t record more than the balance', async () => {
    vi.mocked(getInvoice).mockResolvedValue(partPaid)
    const user = userEvent.setup()
    render(<InvoiceDetailPage id="inv-1" clients={clients} />)

    await user.click(await screen.findByRole('button', { name: 'Record payment' }))
    const amount = screen.getByLabelText('Amount (USD) *')
    await user.clear(amount)
    await user.type(amount, '500')
    await user.click(screen.getByRole('button', { name: 'Record payment' }))

    expect(createPayment).not.toHaveBeenCalled()
    expect(screen.getByText(/This is more than the .* still owed\./)).toBeInTheDocument()
  })

  it('deletes a payment after confirmation', async () => {
    vi.stubGlobal('confirm', vi.fn().mockReturnValue(true))
    vi.mocked(getInvoice).mockResolvedValue(partPaid)
    vi.mocked(deletePayment).mockResolvedValue(undefined)
    const user = userEvent.setup()
    render(<InvoiceDetailPage id="inv-1" clients={clients} />)

    await user.click(await screen.findByRole('button', { name: /Delete payment of/ }))

    await waitFor(() => expect(deletePayment).toHaveBeenCalledWith('inv-1', 'pay-1'))
  })

  it('only offers void and delete while nothing has been paid', async () => {
    vi.mocked(getInvoice).mockResolvedValue(partPaid)

    render(<InvoiceDetailPage id="inv-1" clients={clients} />)

    await screen.findByRole('heading', { name: 'Invoice #1001' })
    expect(screen.queryByRole('button', { name: 'Void invoice' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Delete invoice' })).not.toBeInTheDocument()
  })

  it('offers edit and delete on a draft, but not void', async () => {
    vi.mocked(getInvoice).mockResolvedValue(draft)

    render(<InvoiceDetailPage id="inv-1" clients={clients} />)

    expect(await screen.findByRole('link', { name: 'Edit' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Delete invoice' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Void invoice' })).not.toBeInTheDocument()
  })

  it('freezes a sent invoice: no edit or delete, and voids it through the void action', async () => {
    vi.stubGlobal('confirm', vi.fn().mockReturnValue(true))
    const sent = { ...draft, status: InvoiceStatus.Sent }
    vi.mocked(getInvoice).mockResolvedValue(sent)
    vi.mocked(voidInvoice).mockResolvedValue({ ...sent, status: InvoiceStatus.Void, version: 3 })
    const user = userEvent.setup()
    render(<InvoiceDetailPage id="inv-1" clients={clients} />)

    await user.click(await screen.findByRole('button', { name: 'Void invoice' }))

    expect(screen.queryByRole('link', { name: 'Edit' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Delete invoice' })).not.toBeInTheDocument()
    await waitFor(() => expect(voidInvoice).toHaveBeenCalledWith('inv-1', 2))
    expect(await screen.findByText('Void')).toBeInTheDocument()
  })

  it('offers nothing to change on a void invoice', async () => {
    vi.mocked(getInvoice).mockResolvedValue({ ...draft, status: InvoiceStatus.Void })

    render(<InvoiceDetailPage id="inv-1" clients={clients} />)

    await screen.findByRole('heading', { name: 'Invoice #1001' })
    expect(screen.queryByRole('link', { name: 'Edit' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Void invoice' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Delete invoice' })).not.toBeInTheDocument()
  })

  it('deletes a draft invoice and returns to the list', async () => {
    vi.stubGlobal('confirm', vi.fn().mockReturnValue(true))
    vi.mocked(getInvoice).mockResolvedValue(draft)
    vi.mocked(deleteInvoice).mockResolvedValue(undefined)
    const user = userEvent.setup()
    render(<InvoiceDetailPage id="inv-1" clients={clients} />)

    await user.click(await screen.findByRole('button', { name: 'Delete invoice' }))

    await waitFor(() => expect(deleteInvoice).toHaveBeenCalledWith('inv-1'))
    expect(window.location.hash).toBe('#/invoices')
  })

  it('shows a conflict from the server when the invoice changed underneath', async () => {
    const { ApiError } = await import('../api/http')
    vi.mocked(getInvoice).mockResolvedValue(draft)
    vi.mocked(sendInvoice).mockRejectedValue(new ApiError('The invoice was modified by another request. Reload and try again.', 409))
    const user = userEvent.setup()
    render(<InvoiceDetailPage id="inv-1" clients={clients} />)

    await user.click(await screen.findByRole('button', { name: 'Mark as sent' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('modified by another request')
  })
})

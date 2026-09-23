import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { listClients } from '../api/clientsApi'
import type { Invoice } from '../api/invoicesApi'
import { InvoiceStatus, getInvoice, listInvoices } from '../api/invoicesApi'
import type { LedgerPayment } from '../api/paymentsApi'
import { PaymentMethod, createPayment, deletePayment, listPayments, updatePayment } from '../api/paymentsApi'
import { PaymentsPage } from './PaymentsPage'

vi.mock('../api/clientsApi', () => ({ listClients: vi.fn() }))

vi.mock('../api/invoicesApi', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/invoicesApi')>()),
  listInvoices: vi.fn(),
  getInvoice: vi.fn(),
}))

vi.mock('../api/paymentsApi', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/paymentsApi')>()),
  listPayments: vi.fn(),
  createPayment: vi.fn(),
  updatePayment: vi.fn(),
  deletePayment: vi.fn(),
}))

const ledgerPayment: LedgerPayment = {
  id: 'pay-1',
  invoiceId: 'inv-1',
  invoiceNumber: 1001,
  clientId: 'client-1',
  clientName: 'Acme Corp',
  currency: 'USD',
  amount: 100,
  paymentDate: '2026-09-05T00:00:00+00:00',
  method: PaymentMethod.BankTransfer,
  notes: 'REF-42',
  version: 1,
}

const openInvoice: Invoice = {
  id: 'inv-2',
  clientId: 'client-1',
  invoiceNumber: 1002,
  status: InvoiceStatus.Sent,
  issueDate: '2026-09-01T00:00:00+00:00',
  dueDate: '2026-10-01T00:00:00+00:00',
  currency: 'EUR',
  subTotal: 500,
  taxTotal: 0,
  grandTotal: 500,
  notes: null,
  version: 1,
  items: [],
  payments: [{ id: 'pay-9', invoiceId: 'inv-2', amount: 120, paymentDate: '2026-09-02T00:00:00+00:00', method: 0, notes: null, version: 0 }],
}

beforeEach(() => {
  window.location.hash = '#/payments'
  vi.mocked(listPayments).mockResolvedValue({ items: [ledgerPayment], page: 1, pageSize: 25, totalRecords: 1, totalPages: 1 })
  vi.mocked(listClients).mockResolvedValue([{ id: 'client-1', companyName: 'Acme Corp', email: 'billing@acme.test' }])
  vi.mocked(listInvoices).mockImplementation(async (filter) => ({
    items: filter?.status === InvoiceStatus.Sent ? [openInvoice] : [],
    page: 1,
    pageSize: 200,
    totalRecords: filter?.status === InvoiceStatus.Sent ? 1 : 0,
    totalPages: 1,
  }))
  vi.mocked(getInvoice).mockResolvedValue(openInvoice)
})

afterEach(() => {
  vi.clearAllMocks()
  vi.unstubAllGlobals()
})

describe('PaymentsPage ledger', () => {
  it('lists payments with their client, invoice and method', async () => {
    render(<PaymentsPage route={{ page: 'payments', view: 'list' }} />)

    const row = (await screen.findByText('Acme Corp')).closest('tr') as HTMLElement
    expect(within(row).getByRole('link', { name: '#1001' })).toHaveAttribute('href', '#/invoices/inv-1')
    expect(within(row).getByText('Bank transfer · REF-42')).toBeInTheDocument()
  })

  it('shows an empty state when nothing has been paid', async () => {
    vi.mocked(listPayments).mockResolvedValue({ items: [], page: 1, pageSize: 25, totalRecords: 0, totalPages: 0 })

    render(<PaymentsPage route={{ page: 'payments', view: 'list' }} />)

    expect(await screen.findByRole('heading', { name: 'No payments yet' })).toBeInTheDocument()
  })

  it('edits a payment, allowing up to the invoice balance plus its own amount', async () => {
    vi.mocked(updatePayment).mockResolvedValue(ledgerPayment)
    const user = userEvent.setup()
    render(<PaymentsPage route={{ page: 'payments', view: 'list' }} />)

    await user.click(await screen.findByRole('button', { name: /Edit .* payment from Acme Corp/ }))
    const amount = await screen.findByLabelText('Amount (USD) *')
    // Balance is 500 - 120 = 380, and this payment's own 100 can be reassigned.
    await user.clear(amount)
    await user.type(amount, '481')
    await user.click(screen.getByRole('button', { name: 'Save payment' }))
    expect(updatePayment).not.toHaveBeenCalled()

    await user.clear(amount)
    await user.type(amount, '480')
    await user.click(screen.getByRole('button', { name: 'Save payment' }))

    await waitFor(() =>
      expect(updatePayment).toHaveBeenCalledWith('inv-1', 'pay-1', expect.objectContaining({ amount: 480, version: 1, notes: 'REF-42' })),
    )
    expect(listPayments).toHaveBeenCalledTimes(2)
  })

  it('deletes a payment after confirmation', async () => {
    vi.stubGlobal('confirm', vi.fn().mockReturnValue(true))
    vi.mocked(deletePayment).mockResolvedValue(undefined)
    const user = userEvent.setup()
    render(<PaymentsPage route={{ page: 'payments', view: 'list' }} />)

    await user.click(await screen.findByRole('button', { name: /Delete .* payment from Acme Corp/ }))

    await waitFor(() => expect(deletePayment).toHaveBeenCalledWith('inv-1', 'pay-1'))
  })
})

describe('PaymentsPage record', () => {
  it('records a payment against a chosen open invoice', async () => {
    vi.mocked(createPayment).mockResolvedValue(ledgerPayment)
    const user = userEvent.setup()
    render(<PaymentsPage route={{ page: 'payments', view: 'new' }} />)

    await user.selectOptions(await screen.findByLabelText('Invoice *'), 'inv-2')

    const amount = await screen.findByLabelText('Amount (EUR) *')
    expect(amount).toHaveValue(380)
    await user.click(screen.getByRole('button', { name: 'Record payment' }))

    await waitFor(() =>
      expect(createPayment).toHaveBeenCalledWith('inv-2', expect.objectContaining({ amount: 380, method: PaymentMethod.BankTransfer })),
    )
    expect(window.location.hash).toBe('#/payments')
  })

  it('preselects the invoice passed in the route', async () => {
    render(<PaymentsPage route={{ page: 'payments', view: 'new', invoiceId: 'inv-2' }} />)

    expect(await screen.findByLabelText('Amount (EUR) *')).toHaveValue(380)
    expect(screen.getByLabelText('Invoice *')).toHaveValue('inv-2')
  })

  it('explains when no invoice is waiting on payment', async () => {
    vi.mocked(listInvoices).mockResolvedValue({ items: [], page: 1, pageSize: 200, totalRecords: 0, totalPages: 0 })

    render(<PaymentsPage route={{ page: 'payments', view: 'new' }} />)

    expect(await screen.findByRole('heading', { name: 'Nothing to collect' })).toBeInTheDocument()
  })
})

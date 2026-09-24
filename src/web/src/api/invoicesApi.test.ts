import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Invoice } from './invoicesApi'
import {
  InvoiceStatus,
  balanceDue,
  createInvoice,
  deleteInvoice,
  listInvoices,
  sendInvoice,
  toUpdateInput,
  updateInvoice,
  voidInvoice,
} from './invoicesApi'

const invoice: Invoice = {
  id: 'inv-1',
  clientId: 'client-1',
  invoiceNumber: 1001,
  status: InvoiceStatus.Sent,
  issueDate: '2026-09-01T00:00:00+00:00',
  dueDate: '2026-10-01T00:00:00+00:00',
  currency: 'USD',
  subTotal: 300,
  taxTotal: 30,
  grandTotal: 330,
  notes: null,
  version: 4,
  items: [{ id: 'item-1', description: 'Consulting', quantity: 2, unitPrice: 150, taxRate: 0.1, lineTotal: 300, sortOrder: 0 }],
  payments: [
    { id: 'pay-1', invoiceId: 'inv-1', amount: 100, paymentDate: '2026-09-05T00:00:00+00:00', method: 0, notes: null, version: 0 },
    { id: 'pay-2', invoiceId: 'inv-1', amount: 30.1, paymentDate: '2026-09-06T00:00:00+00:00', method: 1, notes: null, version: 0 },
  ],
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn())
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('listInvoices', () => {
  it('sends the status by name, which is what the API binds', async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(JSON.stringify({ items: [], page: 1, pageSize: 25, totalRecords: 0, totalPages: 0 })))

    await listInvoices({ status: InvoiceStatus.Overdue, page: 1, pageSize: 25 })

    expect(vi.mocked(fetch).mock.calls[0][0]).toBe('/bff/invoices?status=Overdue&page=1&pageSize=25')
  })
})

describe('createInvoice', () => {
  it('posts the invoice as JSON', async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(JSON.stringify(invoice), { status: 201 }))
    const input = { clientId: 'client-1', issueDate: 'a', dueDate: 'b', currency: 'USD', notes: null, items: [] }

    await createInvoice(input)

    expect(fetch).toHaveBeenCalledWith('/bff/invoices', expect.objectContaining({ method: 'POST', body: JSON.stringify(input) }))
  })
})

describe('updateInvoice and deleteInvoice', () => {
  it('target the invoice by id', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(new Response(JSON.stringify(invoice)))
    vi.mocked(fetch).mockResolvedValueOnce(new Response(null, { status: 204 }))

    await updateInvoice('inv-1', toUpdateInput(invoice))
    await deleteInvoice('inv-1')

    expect(fetch).toHaveBeenNthCalledWith(1, '/bff/invoices/inv-1', expect.objectContaining({ method: 'PUT' }))
    expect(fetch).toHaveBeenNthCalledWith(2, '/bff/invoices/inv-1', expect.objectContaining({ method: 'DELETE' }))
  })
})

describe('sendInvoice and voidInvoice', () => {
  it('post to the action endpoint with the version the invoice was loaded at', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(new Response(JSON.stringify(invoice)))
    vi.mocked(fetch).mockResolvedValueOnce(new Response(JSON.stringify(invoice)))

    await sendInvoice('inv-1', 4)
    await voidInvoice('inv-1', 5)

    expect(fetch).toHaveBeenNthCalledWith(
      1,
      '/bff/invoices/inv-1/send',
      expect.objectContaining({ method: 'POST', body: JSON.stringify({ version: 4 }) }),
    )
    expect(fetch).toHaveBeenNthCalledWith(
      2,
      '/bff/invoices/inv-1/void',
      expect.objectContaining({ method: 'POST', body: JSON.stringify({ version: 5 }) }),
    )
  })
})

describe('toUpdateInput', () => {
  it('keeps the loaded invoice, its line ids and version, and applies the changes, without a status', () => {
    const input = toUpdateInput(invoice, { notes: 'Net 30' })

    expect(input).toEqual({
      clientId: 'client-1',
      issueDate: invoice.issueDate,
      dueDate: invoice.dueDate,
      currency: 'USD',
      notes: 'Net 30',
      version: 4,
      items: [{ id: 'item-1', description: 'Consulting', quantity: 2, unitPrice: 150, taxRate: 0.1, sortOrder: 0 }],
    })
  })
})

describe('balanceDue', () => {
  it('subtracts payments without floating-point drift', () => {
    expect(balanceDue(invoice)).toBe(199.9)
  })
})

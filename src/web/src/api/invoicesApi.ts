import type { Payment } from './paymentsApi'
import { roundMoney } from '../lib/format'
import { requestJson, requestNoContent, withQuery } from './http'

// Mirrors InvoicingApi's InvoiceStatus, which travels over the wire as its number. The API never stores
// Overdue: it reports a Sent invoice past its due date as Overdue.
export const InvoiceStatus = {
  Draft: 0,
  Sent: 1,
  Paid: 2,
  Overdue: 3,
  Void: 4,
} as const
export type InvoiceStatus = (typeof InvoiceStatus)[keyof typeof InvoiceStatus]

export const invoiceStatusLabels: Record<InvoiceStatus, string> = {
  [InvoiceStatus.Draft]: 'Draft',
  [InvoiceStatus.Sent]: 'Sent',
  [InvoiceStatus.Paid]: 'Paid',
  [InvoiceStatus.Overdue]: 'Overdue',
  [InvoiceStatus.Void]: 'Void',
}

export interface InvoiceSummary {
  id: string
  clientId: string
  invoiceNumber: number
  status: InvoiceStatus
  issueDate: string
  dueDate: string
  currency: string
  grandTotal: number
}

export interface InvoiceList {
  items: InvoiceSummary[]
  page: number
  pageSize: number
  totalRecords: number
  totalPages: number
}

export interface InvoiceItem {
  id: string
  description: string
  quantity: number
  unitPrice: number
  taxRate: number
  lineTotal: number
  sortOrder: number
}

export interface Invoice {
  id: string
  clientId: string
  invoiceNumber: number
  status: InvoiceStatus
  issueDate: string
  dueDate: string
  currency: string
  subTotal: number
  taxTotal: number
  grandTotal: number
  notes: string | null
  version: number
  items: InvoiceItem[]
  payments: Payment[]
}

export interface InvoiceItemInput {
  // Present for an existing line on update so the API edits it in place instead of replacing it.
  id?: string | null
  description: string
  quantity: number
  unitPrice: number
  // A fraction: 0.1 means 10%.
  taxRate: number
  sortOrder: number
}

export interface CreateInvoiceInput {
  clientId: string
  issueDate: string
  dueDate: string
  currency: string
  notes: string | null
  items: InvoiceItemInput[]
}

// Status isn't part of an update: it changes through sendInvoice/voidInvoice, or with payments.
export interface UpdateInvoiceInput extends CreateInvoiceInput {
  version: number
}

export interface ListInvoicesFilter {
  clientId?: string
  status?: InvoiceStatus
  page?: number
  pageSize?: number
}

export function listInvoices(filter: ListInvoicesFilter = {}): Promise<InvoiceList> {
  const status = filter.status === undefined ? undefined : invoiceStatusLabels[filter.status]
  return requestJson<InvoiceList>(
    withQuery('/bff/invoices', { clientId: filter.clientId, status, page: filter.page, pageSize: filter.pageSize }),
  )
}

export function getInvoice(id: string): Promise<Invoice> {
  return requestJson<Invoice>(`/bff/invoices/${id}`)
}

export function createInvoice(input: CreateInvoiceInput): Promise<Invoice> {
  return requestJson<Invoice>('/bff/invoices', { method: 'POST', body: JSON.stringify(input) })
}

export function updateInvoice(id: string, input: UpdateInvoiceInput): Promise<Invoice> {
  return requestJson<Invoice>(`/bff/invoices/${id}`, { method: 'PUT', body: JSON.stringify(input) })
}

export function deleteInvoice(id: string): Promise<void> {
  return requestNoContent(`/bff/invoices/${id}`, { method: 'DELETE' })
}

// Draft → Sent. From then on only the notes can change.
export function sendInvoice(id: string, version: number): Promise<Invoice> {
  return requestJson<Invoice>(`/bff/invoices/${id}/send`, { method: 'POST', body: JSON.stringify({ version }) })
}

// Sent → Void, only while nothing has been paid.
export function voidInvoice(id: string, version: number): Promise<Invoice> {
  return requestJson<Invoice>(`/bff/invoices/${id}/void`, { method: 'POST', body: JSON.stringify({ version }) })
}

// Turns a loaded invoice back into an update payload, so an edit keeps everything it doesn't change.
export function toUpdateInput(invoice: Invoice, changes: Partial<UpdateInvoiceInput> = {}): UpdateInvoiceInput {
  return {
    clientId: invoice.clientId,
    issueDate: invoice.issueDate,
    dueDate: invoice.dueDate,
    currency: invoice.currency,
    notes: invoice.notes,
    version: invoice.version,
    items: invoice.items.map((item) => ({
      id: item.id,
      description: item.description,
      quantity: item.quantity,
      unitPrice: item.unitPrice,
      taxRate: item.taxRate,
      sortOrder: item.sortOrder,
    })),
    ...changes,
  }
}

export function amountPaid(invoice: Invoice): number {
  return roundMoney(invoice.payments.reduce((sum, payment) => sum + payment.amount, 0))
}

export function balanceDue(invoice: Invoice): number {
  return roundMoney(invoice.grandTotal - amountPaid(invoice))
}

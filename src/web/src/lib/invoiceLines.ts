import type { CreateInvoiceInput, Invoice } from '../api/invoicesApi'
import { fromDateInput, roundMoney, toDateInput } from './format'

export interface InvoiceLineValues {
  // Stable React key for the row; `id` is only set for lines that already exist on the server.
  key: string
  id: string | null
  description: string
  quantity: string
  unitPrice: string
  // Entered as a percentage (10 means 10%); converted to the API's fraction on submit.
  taxPercent: string
}

export interface InvoiceFormValues {
  clientId: string
  issueDate: string
  dueDate: string
  currency: string
  notes: string
  items: InvoiceLineValues[]
}

let nextLineKey = 0
export function newLine(): InvoiceLineValues {
  nextLineKey += 1
  return { key: `line-${nextLineKey}`, id: null, description: '', quantity: '1', unitPrice: '', taxPercent: '0' }
}

export function parseNumber(value: string): number | null {
  if (value.trim() === '') {
    return null
  }
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : null
}

export function lineAmount(line: InvoiceLineValues): number {
  return roundMoney((parseNumber(line.quantity) ?? 0) * (parseNumber(line.unitPrice) ?? 0))
}

// The API stores tax as a fraction (0.07); the form shows a percentage. Rounded to strip float noise
// like 0.07 * 100 = 7.000000000000001.
export function taxRateToPercent(taxRate: number): number {
  return Math.round(taxRate * 100 * 10_000) / 10_000
}

// A preview only: the API recalculates every total when it saves, with the same rounding.
export function calculateTotals(items: InvoiceLineValues[]) {
  let subTotal = 0
  let taxTotal = 0
  for (const line of items) {
    const lineTotal = lineAmount(line)
    subTotal += lineTotal
    taxTotal += roundMoney(lineTotal * ((parseNumber(line.taxPercent) ?? 0) / 100))
  }
  return { subTotal: roundMoney(subTotal), taxTotal: roundMoney(taxTotal), grandTotal: roundMoney(subTotal + taxTotal) }
}

export function toInvoiceInput(values: InvoiceFormValues): CreateInvoiceInput {
  return {
    clientId: values.clientId,
    issueDate: fromDateInput(values.issueDate),
    dueDate: fromDateInput(values.dueDate),
    currency: values.currency,
    notes: values.notes || null,
    items: values.items.map((line, index) => ({
      id: line.id,
      description: line.description,
      quantity: Number(line.quantity),
      unitPrice: Number(line.unitPrice),
      taxRate: Number(line.taxPercent) / 100,
      sortOrder: index,
    })),
  }
}

export function invoiceToFormValues(invoice: Invoice): InvoiceFormValues {
  return {
    clientId: invoice.clientId,
    issueDate: toDateInput(invoice.issueDate),
    dueDate: toDateInput(invoice.dueDate),
    currency: invoice.currency,
    notes: invoice.notes ?? '',
    items: invoice.items.map((item) => ({
      ...newLine(),
      id: item.id,
      description: item.description,
      quantity: String(item.quantity),
      unitPrice: String(item.unitPrice),
      taxPercent: String(taxRateToPercent(item.taxRate)),
    })),
  }
}

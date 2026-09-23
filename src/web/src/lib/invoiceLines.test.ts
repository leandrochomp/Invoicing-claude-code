import { describe, expect, it } from 'vitest'
import type { Invoice } from '../api/invoicesApi'
import type { InvoiceFormValues } from './invoiceLines'
import { invoiceToFormValues, lineAmount, newLine, taxRateToPercent, toInvoiceInput } from './invoiceLines'

describe('invoice line helpers', () => {
  it('computes a rounded line amount, treating blanks as 0', () => {
    expect(lineAmount({ ...newLine(), quantity: '3', unitPrice: '0.335' })).toBe(1.01)
    expect(lineAmount({ ...newLine(), quantity: '2', unitPrice: '' })).toBe(0)
  })

  it('turns a tax fraction into a percentage without float noise', () => {
    expect(taxRateToPercent(0.07)).toBe(7)
    expect(taxRateToPercent(0.125)).toBe(12.5)
  })
})

describe('invoice form conversion', () => {
  const values: InvoiceFormValues = {
    clientId: 'c1',
    issueDate: '2026-09-01',
    dueDate: '2026-10-01',
    currency: 'AUD',
    notes: '',
    items: [
      { ...newLine(), id: 'i1', description: 'Consulting', quantity: '2', unitPrice: '150', taxPercent: '10' },
      { ...newLine(), description: 'Travel', quantity: '1', unitPrice: '40', taxPercent: '0' },
    ],
  }

  it('builds the API input with UTC dates, tax fractions and sort order', () => {
    expect(toInvoiceInput(values)).toEqual({
      clientId: 'c1',
      issueDate: '2026-09-01T00:00:00Z',
      dueDate: '2026-10-01T00:00:00Z',
      currency: 'AUD',
      notes: null,
      items: [
        { id: 'i1', description: 'Consulting', quantity: 2, unitPrice: 150, taxRate: 0.1, sortOrder: 0 },
        { id: null, description: 'Travel', quantity: 1, unitPrice: 40, taxRate: 0, sortOrder: 1 },
      ],
    })
  })

  it('loads a saved invoice back into form values', () => {
    const invoice = {
      clientId: 'c1',
      issueDate: '2026-09-01T00:00:00Z',
      dueDate: '2026-10-01T00:00:00Z',
      currency: 'AUD',
      notes: null,
      items: [{ id: 'i1', description: 'Consulting', quantity: 2, unitPrice: 150, taxRate: 0.07, lineTotal: 300, sortOrder: 0 }],
    } as unknown as Invoice

    const loaded = invoiceToFormValues(invoice)

    expect(loaded).toMatchObject({ clientId: 'c1', issueDate: '2026-09-01', dueDate: '2026-10-01', currency: 'AUD', notes: '' })
    expect(loaded.items).toEqual([
      expect.objectContaining({ id: 'i1', description: 'Consulting', quantity: '2', unitPrice: '150', taxPercent: '7' }),
    ])
  })
})

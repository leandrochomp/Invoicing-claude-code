import { describe, expect, it } from 'vitest'
import { parseRoute } from './route'

describe('parseRoute', () => {
  it.each([
    ['', { page: 'home' }],
    ['#/', { page: 'home' }],
    ['#/nowhere', { page: 'home' }],
    ['#/clients', { page: 'clients' }],
    ['#/invoices', { page: 'invoices', view: 'list' }],
    ['#/invoices/new', { page: 'invoices', view: 'new' }],
    ['#/invoices/abc', { page: 'invoices', view: 'detail', id: 'abc' }],
    ['#/invoices/abc/edit', { page: 'invoices', view: 'edit', id: 'abc' }],
    ['#/payments', { page: 'payments', view: 'list' }],
    ['#/payments/new', { page: 'payments', view: 'new', invoiceId: undefined }],
    ['#/payments/new?invoiceId=abc', { page: 'payments', view: 'new', invoiceId: 'abc' }],
  ])('parses %s', (hash, expected) => {
    expect(parseRoute(hash)).toEqual(expected)
  })
})

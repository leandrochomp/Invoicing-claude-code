import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { PaymentMethod, createPayment, deletePayment, listPayments, updatePayment } from './paymentsApi'

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn())
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('paymentsApi', () => {
  it('lists the ledger with paging', async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(JSON.stringify({ items: [], page: 2, pageSize: 25, totalRecords: 0, totalPages: 0 })))

    await listPayments({ page: 2, pageSize: 25 })

    expect(vi.mocked(fetch).mock.calls[0][0]).toBe('/bff/payments?page=2&pageSize=25')
  })

  it('records a payment under its invoice', async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(JSON.stringify({ id: 'pay-1' }), { status: 201 }))
    const input = { amount: 50, paymentDate: '2026-09-10T00:00:00Z', method: PaymentMethod.Card, notes: null }

    await createPayment('inv-1', input)

    expect(fetch).toHaveBeenCalledWith(
      '/bff/invoices/inv-1/payments',
      expect.objectContaining({ method: 'POST', body: JSON.stringify(input) }),
    )
  })

  it('updates and deletes a payment by invoice and payment id', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(new Response(JSON.stringify({ id: 'pay-1' })))
    vi.mocked(fetch).mockResolvedValueOnce(new Response(null, { status: 204 }))

    await updatePayment('inv-1', 'pay-1', { amount: 60, paymentDate: 'x', method: PaymentMethod.Cash, notes: null, version: 2 })
    await deletePayment('inv-1', 'pay-1')

    expect(fetch).toHaveBeenNthCalledWith(1, '/bff/invoices/inv-1/payments/pay-1', expect.objectContaining({ method: 'PUT' }))
    expect(fetch).toHaveBeenNthCalledWith(2, '/bff/invoices/inv-1/payments/pay-1', expect.objectContaining({ method: 'DELETE' }))
  })
})

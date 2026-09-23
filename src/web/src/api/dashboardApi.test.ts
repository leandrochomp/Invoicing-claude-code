import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getDashboard } from './dashboardApi'

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn())
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('getDashboard', () => {
  it('fetches the summary from the BFF with cookies included', async () => {
    const summary = { totals: [], counts: { draft: 0, outstanding: 0, overdue: 0, paid: 0 }, dueInvoices: [], recentPayments: [] }
    vi.mocked(fetch).mockResolvedValue(new Response(JSON.stringify(summary)))

    await expect(getDashboard()).resolves.toEqual(summary)
    expect(fetch).toHaveBeenCalledWith('/bff/dashboard', expect.objectContaining({ credentials: 'include' }))
  })
})

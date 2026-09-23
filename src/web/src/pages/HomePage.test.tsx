import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { DashboardSummary } from '../api/dashboardApi'
import { getDashboard } from '../api/dashboardApi'
import { InvoiceStatus } from '../api/invoicesApi'
import { PaymentMethod } from '../api/paymentsApi'
import { formatMoney } from '../lib/format'
import { HomePage } from './HomePage'

vi.mock('../api/dashboardApi', () => ({ getDashboard: vi.fn() }))

const summary: DashboardSummary = {
  totals: [{ currency: 'USD', outstanding: 1100, overdue: 600, collectedLast30Days: 400 }],
  counts: { draft: 1, outstanding: 2, overdue: 1, paid: 3 },
  dueInvoices: [
    {
      id: 'inv-1',
      invoiceNumber: 1001,
      clientId: 'client-1',
      clientName: 'Acme Corp',
      status: InvoiceStatus.Sent,
      dueDate: '2026-09-01T00:00:00+00:00',
      currency: 'USD',
      grandTotal: 1000,
      amountDue: 600,
      isOverdue: true,
    },
  ],
  recentPayments: [
    {
      id: 'pay-1',
      invoiceId: 'inv-1',
      invoiceNumber: 1001,
      clientId: 'client-1',
      clientName: 'Globex',
      currency: 'USD',
      amount: 400,
      paymentDate: '2026-09-10T00:00:00+00:00',
      method: PaymentMethod.Card,
      notes: null,
      version: 0,
    },
  ],
}

afterEach(() => {
  vi.clearAllMocks()
})

function statValue(label: string) {
  return screen.getByText(label).nextElementSibling?.textContent
}

describe('HomePage', () => {
  it('greets the user and shows what is owed, overdue and collected', async () => {
    vi.mocked(getDashboard).mockResolvedValue(summary)

    render(<HomePage username="alice" />)

    expect(screen.getByRole('heading', { name: 'Welcome back, alice' })).toBeInTheDocument()
    await screen.findByText('Outstanding')
    expect(statValue('Outstanding')).toBe(formatMoney(1100, 'USD'))
    expect(statValue('Overdue')).toBe(formatMoney(600, 'USD'))
    expect(statValue('Collected, last 30 days')).toBe(formatMoney(400, 'USD'))
    expect(screen.getByText(/2 awaiting payment/)).toBeInTheDocument()
  })

  it('links invoices coming due and recent payments to their invoice', async () => {
    vi.mocked(getDashboard).mockResolvedValue(summary)

    render(<HomePage username="alice" />)

    const due = within(await screen.findByRole('region', { name: 'Coming due' }))
    expect(due.getByRole('link', { name: /Acme Corp/ })).toHaveAttribute('href', '#/invoices/inv-1')
    expect(due.getByText(/overdue since/)).toBeInTheDocument()
    const recent = within(screen.getByRole('region', { name: 'Recent payments' }))
    expect(recent.getByRole('link', { name: /Globex/ })).toHaveAttribute('href', '#/invoices/inv-1')
  })

  it('labels each row with its currency when there is more than one', async () => {
    vi.mocked(getDashboard).mockResolvedValue({
      ...summary,
      totals: [...summary.totals, { currency: 'EUR', outstanding: 50, overdue: 0, collectedLast30Days: 0 }],
    })

    render(<HomePage username="alice" />)

    expect(await screen.findByText('Outstanding · EUR')).toBeInTheDocument()
    expect(screen.getByText('Outstanding · USD')).toBeInTheDocument()
  })

  it('shows a getting-started state before anything is billed', async () => {
    vi.mocked(getDashboard).mockResolvedValue({
      totals: [],
      counts: { draft: 0, outstanding: 0, overdue: 0, paid: 0 },
      dueInvoices: [],
      recentPayments: [],
    })

    render(<HomePage username="alice" />)

    expect(await screen.findByRole('heading', { name: 'Nothing billed yet' })).toBeInTheDocument()
  })

  it('lets the user retry when the summary fails to load', async () => {
    const { ApiError } = await import('../api/http')
    vi.mocked(getDashboard).mockRejectedValueOnce(new ApiError('Unable to reach the Invoicing API.', 503)).mockResolvedValue(summary)
    const user = userEvent.setup()
    render(<HomePage username="alice" />)

    await user.click(await screen.findByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('Outstanding')).toBeInTheDocument()
  })
})

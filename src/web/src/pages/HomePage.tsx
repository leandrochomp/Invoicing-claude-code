import { useEffect, useState } from 'react'
import type { DashboardSummary } from '../api/dashboardApi'
import { getDashboard } from '../api/dashboardApi'
import { errorMessage } from '../api/http'
import { paymentMethodLabels } from '../api/paymentsApi'
import { ErrorAlert } from '../components/ErrorAlert'
import { LoadingRows } from '../components/LoadingRows'
import { formatDate, formatInvoiceNumber, formatMoney } from '../lib/format'

export function HomePage({ username }: { username: string }) {
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [reloadToken, setReloadToken] = useState(0)

  useEffect(() => {
    setError(null)
    getDashboard()
      .then(setSummary)
      .catch((err) => setError(errorMessage(err, 'Failed to load your summary.')))
  }, [reloadToken])

  const isEmpty =
    summary &&
    summary.totals.length === 0 &&
    summary.counts.draft === 0 &&
    summary.counts.outstanding === 0 &&
    summary.counts.paid === 0

  return (
    <main>
      <header className="page-header page-header-wrap">
        <div>
          <h1>Welcome back, {username}</h1>
          <p className="page-intro">What’s owed, what’s late and what’s come in.</p>
        </div>
        <div className="header-actions">
          <a className="button-secondary" href="#/payments/new">
            Record payment
          </a>
          <a className="button-primary" href="#/invoices/new">
            New invoice
          </a>
        </div>
      </header>

      {error && <ErrorAlert message={error} onRetry={() => setReloadToken((token) => token + 1)} />}
      {!summary && !error && <LoadingRows label="Loading your summary…" />}

      {isEmpty && (
        <div className="panel empty-state">
          <h2>Nothing billed yet</h2>
          <p>Add a client, then create your first invoice. Totals and due dates will show up here.</p>
          <div className="empty-state-actions">
            <a className="button-secondary" href="#/clients">
              Add a client
            </a>
            <a className="button-primary" href="#/invoices/new">
              Create an invoice
            </a>
          </div>
        </div>
      )}

      {summary && !isEmpty && (
        <>
          <section aria-labelledby="totals-title" className="home-section">
            <h2 id="totals-title" className="visually-hidden">
              Totals by currency
            </h2>
            {summary.totals.length === 0 ? (
              <p className="panel panel-empty">No money is owed right now and nothing came in over the last 30 days.</p>
            ) : (
              summary.totals.map((totals) => (
                <dl key={totals.currency} className="stat-row" aria-label={`${totals.currency} totals`}>
                  <div className="stat">
                    <dt>Outstanding{summary.totals.length > 1 ? ` · ${totals.currency}` : ''}</dt>
                    <dd>{formatMoney(totals.outstanding, totals.currency)}</dd>
                  </div>
                  <div className={totals.overdue > 0 ? 'stat stat-alert' : 'stat'}>
                    <dt>Overdue</dt>
                    <dd>{formatMoney(totals.overdue, totals.currency)}</dd>
                  </div>
                  <div className="stat">
                    <dt>Collected, last 30 days</dt>
                    <dd>{formatMoney(totals.collectedLast30Days, totals.currency)}</dd>
                  </div>
                </dl>
              ))
            )}
            <p className="stat-counts">
              <a href="#/invoices">
                {summary.counts.outstanding} awaiting payment
                {summary.counts.overdue > 0 && <strong className="text-danger"> · {summary.counts.overdue} overdue</strong>}
                {' · '}
                {summary.counts.draft} {summary.counts.draft === 1 ? 'draft' : 'drafts'} · {summary.counts.paid} paid
              </a>
            </p>
          </section>

          <div className="home-grid">
            <section className="panel panel-flush" aria-labelledby="due-title">
              <h2 id="due-title" className="panel-title panel-title-padded">
                Coming due
              </h2>
              {summary.dueInvoices.length === 0 ? (
                <p className="panel-empty">No sent invoices are waiting on payment.</p>
              ) : (
                <ul className="summary-list">
                  {summary.dueInvoices.map((invoice) => (
                    <li key={invoice.id}>
                      <a href={`#/invoices/${invoice.id}`}>
                        <span>
                          <strong>{invoice.clientName}</strong>
                          <span className="data-table-muted">
                            {formatInvoiceNumber(invoice.invoiceNumber)} ·{' '}
                            {invoice.isOverdue ? (
                              <span className="text-danger">overdue since {formatDate(invoice.dueDate)}</span>
                            ) : (
                              `due ${formatDate(invoice.dueDate)}`
                            )}
                          </span>
                        </span>
                        <span className="numeric">{formatMoney(invoice.amountDue, invoice.currency)}</span>
                      </a>
                    </li>
                  ))}
                </ul>
              )}
            </section>

            <section className="panel panel-flush" aria-labelledby="recent-title">
              <h2 id="recent-title" className="panel-title panel-title-padded">
                Recent payments
              </h2>
              {summary.recentPayments.length === 0 ? (
                <p className="panel-empty">No payments recorded yet.</p>
              ) : (
                <ul className="summary-list">
                  {summary.recentPayments.map((payment) => (
                    <li key={payment.id}>
                      <a href={`#/invoices/${payment.invoiceId}`}>
                        <span>
                          <strong>{payment.clientName}</strong>
                          <span className="data-table-muted">
                            {formatDate(payment.paymentDate)} · {paymentMethodLabels[payment.method]}
                          </span>
                        </span>
                        <span className="numeric">{formatMoney(payment.amount, payment.currency)}</span>
                      </a>
                    </li>
                  ))}
                </ul>
              )}
              <p className="panel-footer">
                <a href="#/payments">All payments</a>
              </p>
            </section>
          </div>
        </>
      )}
    </main>
  )
}

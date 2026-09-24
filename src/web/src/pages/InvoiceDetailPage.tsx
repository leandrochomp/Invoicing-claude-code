import { useCallback, useEffect, useState } from 'react'
import type { ClientSummary } from '../api/clientsApi'
import { errorMessage } from '../api/http'
import type { Invoice } from '../api/invoicesApi'
import { InvoiceStatus, amountPaid, balanceDue, deleteInvoice, getInvoice, sendInvoice, voidInvoice } from '../api/invoicesApi'
import type { Payment } from '../api/paymentsApi'
import { createPayment, deletePayment, paymentMethodLabels, updatePayment } from '../api/paymentsApi'
import { BackLink } from '../components/BackLink'
import { ErrorAlert } from '../components/ErrorAlert'
import { LoadingRows } from '../components/LoadingRows'
import type { PaymentFormValues } from '../components/PaymentForm'
import { PaymentForm } from '../components/PaymentForm'
import { StatusBadge } from '../components/StatusBadge'
import { formatDate, formatInvoiceNumber, formatMoney } from '../lib/format'
import { clientName } from '../lib/clients'
import { taxRateToPercent } from '../lib/invoiceLines'
import { editableMax, newPaymentFormValues, paymentToFormValues, toPaymentInput } from '../lib/payments'
import { navigate } from '../lib/route'

type PaymentPanel = { mode: 'closed' } | { mode: 'create' } | { mode: 'edit'; payment: Payment }

export function InvoiceDetailPage({ id, clients }: { id: string; clients: ClientSummary[] | null }) {
  const [invoice, setInvoice] = useState<Invoice | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [panel, setPanel] = useState<PaymentPanel>({ mode: 'closed' })
  const [paymentError, setPaymentError] = useState<string | null>(null)

  const reload = useCallback(async () => {
    try {
      setInvoice(await getInvoice(id))
      setLoadError(null)
    } catch (err) {
      setLoadError(errorMessage(err, 'Failed to load the invoice.'))
    }
  }, [id])

  useEffect(() => {
    reload()
  }, [reload])

  if (loadError) {
    return (
      <main>
        <BackLink label="Back to invoices" onClick={() => navigate('/invoices')} />
        <ErrorAlert message={loadError} onRetry={reload} />
      </main>
    )
  }
  if (!invoice) {
    return <LoadingRows label="Loading invoice…" />
  }

  const current = invoice
  const number = formatInvoiceNumber(current.invoiceNumber)
  const paid = amountPaid(current)
  const balance = balanceDue(current)
  const isDraft = current.status === InvoiceStatus.Draft
  const isOpen = current.status === InvoiceStatus.Sent || current.status === InvoiceStatus.Overdue
  const money = (amount: number) => formatMoney(amount, current.currency)

  async function runAction(action: () => Promise<void>, fallback: string) {
    setBusy(true)
    setActionError(null)
    try {
      await action()
    } catch (err) {
      setActionError(errorMessage(err, fallback))
    } finally {
      setBusy(false)
    }
  }

  function handleSend() {
    runAction(async () => {
      setInvoice(await sendInvoice(current.id, current.version))
    }, 'Failed to send the invoice.')
  }

  function handleVoid() {
    if (!window.confirm(`Void invoice ${number}? It stays on record but can no longer be paid.`)) {
      return
    }
    runAction(async () => {
      setInvoice(await voidInvoice(current.id, current.version))
    }, 'Failed to void the invoice.')
  }

  function handleDelete() {
    if (!window.confirm(`Delete invoice ${number}? This can’t be undone.`)) {
      return
    }
    runAction(async () => {
      await deleteInvoice(current.id)
      navigate('/invoices')
    }, 'Failed to delete the invoice.')
  }

  function openPayment(next: PaymentPanel) {
    setPaymentError(null)
    setPanel(next)
  }

  async function handlePaymentSubmit(values: PaymentFormValues) {
    setBusy(true)
    setPaymentError(null)
    const input = toPaymentInput(values)
    try {
      if (panel.mode === 'edit') {
        await updatePayment(current.id, panel.payment.id, { ...input, version: panel.payment.version })
      } else {
        await createPayment(current.id, input)
      }
      setPanel({ mode: 'closed' })
      await reload()
    } catch (err) {
      setPaymentError(errorMessage(err, 'Failed to save the payment.'))
    } finally {
      setBusy(false)
    }
  }

  function handlePaymentDelete(payment: Payment) {
    if (!window.confirm(`Delete the ${money(payment.amount)} payment from ${formatDate(payment.paymentDate)}?`)) {
      return
    }
    runAction(async () => {
      await deletePayment(current.id, payment.id)
      await reload()
    }, 'Failed to delete the payment.')
  }

  return (
    <main>
      <BackLink label="Back to invoices" onClick={() => navigate('/invoices')} />
      <header className="page-header page-header-wrap">
        <div>
          <div className="title-row">
            <h1>Invoice {number}</h1>
            <StatusBadge status={current.status} />
          </div>
          <p className="page-intro">
            {clientName(clients, current.clientId)} · Issued {formatDate(current.issueDate)} · Due {formatDate(current.dueDate)}
          </p>
        </div>
        <div className="header-actions">
          {isDraft && (
            <a className="button-secondary" href={`#/invoices/${current.id}/edit`}>
              Edit
            </a>
          )}
          {isDraft && (
            <button type="button" className="button-primary" onClick={handleSend} disabled={busy}>
              Mark as sent
            </button>
          )}
          {isOpen && balance > 0 && panel.mode === 'closed' && (
            <button type="button" className="button-primary" onClick={() => openPayment({ mode: 'create' })} disabled={busy}>
              Record payment
            </button>
          )}
        </div>
      </header>

      {actionError && <ErrorAlert message={actionError} />}

      <div className="detail-layout">
        <section className="panel panel-flush" aria-labelledby="items-title">
          <h2 id="items-title" className="panel-title panel-title-padded">
            Line items
          </h2>
          <table className="data-table items-table">
            <thead>
              <tr>
                <th scope="col">Description</th>
                <th scope="col" className="numeric">
                  Qty
                </th>
                <th scope="col" className="numeric">
                  Unit price
                </th>
                <th scope="col" className="numeric">
                  Tax
                </th>
                <th scope="col" className="numeric">
                  Amount
                </th>
              </tr>
            </thead>
            <tbody>
              {current.items.map((item) => (
                <tr key={item.id}>
                  <td className="data-table-primary">{item.description}</td>
                  <td className="numeric">{item.quantity}</td>
                  <td className="numeric">{money(item.unitPrice)}</td>
                  <td className="numeric data-table-muted">{taxRateToPercent(item.taxRate)}%</td>
                  <td className="numeric">{money(item.lineTotal)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <dl className="totals totals-padded">
            <div>
              <dt>Subtotal</dt>
              <dd>{money(current.subTotal)}</dd>
            </div>
            <div>
              <dt>Tax</dt>
              <dd>{money(current.taxTotal)}</dd>
            </div>
            <div className="totals-grand">
              <dt>Total</dt>
              <dd>{money(current.grandTotal)}</dd>
            </div>
            {paid > 0 && (
              <div>
                <dt>Paid</dt>
                <dd>−{money(paid)}</dd>
              </div>
            )}
            {current.status !== InvoiceStatus.Draft && current.status !== InvoiceStatus.Void && (
              <div className="totals-balance">
                <dt>Balance due</dt>
                <dd>{money(balance)}</dd>
              </div>
            )}
          </dl>
          {current.notes && (
            <div className="invoice-notes">
              <h3>Notes</h3>
              <p>{current.notes}</p>
            </div>
          )}
        </section>

        <section aria-labelledby="payments-title" className="detail-side">
          {panel.mode !== 'closed' ? (
            <PaymentForm
              key={panel.mode === 'edit' ? panel.payment.id : 'new'}
              heading={panel.mode === 'edit' ? 'Edit payment' : 'Record payment'}
              currency={current.currency}
              maxAmount={panel.mode === 'edit' ? editableMax(current, panel.payment) : balance}
              initialValues={
                panel.mode === 'edit' ? paymentToFormValues(panel.payment) : newPaymentFormValues(current.id, balance)
              }
              submitLabel={panel.mode === 'edit' ? 'Save payment' : 'Record payment'}
              submitting={busy}
              error={paymentError}
              onSubmit={handlePaymentSubmit}
              onCancel={() => setPanel({ mode: 'closed' })}
            />
          ) : null}

          <div className="panel panel-flush">
            <h2 id="payments-title" className="panel-title panel-title-padded">
              Payments
            </h2>
            {current.payments.length === 0 ? (
              <p className="panel-empty">
                {current.status === InvoiceStatus.Draft
                  ? 'Mark this invoice as sent to start recording payments.'
                  : current.status === InvoiceStatus.Void
                    ? 'This invoice is void.'
                    : 'No payments recorded yet.'}
              </p>
            ) : (
              <ul className="payment-list">
                {current.payments
                  .toSorted((a, b) => b.paymentDate.localeCompare(a.paymentDate))
                  .map((payment) => (
                    <li key={payment.id}>
                      <div>
                        <strong>{money(payment.amount)}</strong>
                        <span className="data-table-muted">
                          {formatDate(payment.paymentDate)} · {paymentMethodLabels[payment.method]}
                          {payment.notes ? ` · ${payment.notes}` : ''}
                        </span>
                      </div>
                      <div className="data-table-actions">
                        <button
                          type="button"
                          className="button-ghost"
                          onClick={() => openPayment({ mode: 'edit', payment })}
                          aria-label={`Edit payment of ${money(payment.amount)}`}
                        >
                          Edit
                        </button>
                        <button
                          type="button"
                          className="button-ghost button-ghost-danger"
                          onClick={() => handlePaymentDelete(payment)}
                          aria-label={`Delete payment of ${money(payment.amount)}`}
                          disabled={busy}
                        >
                          Delete
                        </button>
                      </div>
                    </li>
                  ))}
              </ul>
            )}
          </div>

          {/* A draft was never issued, so it can be deleted. Once sent it's on record: it can only be voided,
              and only while nothing has been paid. Paid and void invoices are final. */}
          {(isDraft || (isOpen && current.payments.length === 0)) && (
            <div className="danger-zone">
              {isOpen && (
                <button type="button" className="button-ghost button-ghost-danger" onClick={handleVoid} disabled={busy}>
                  Void invoice
                </button>
              )}
              {isDraft && (
                <button type="button" className="button-ghost button-ghost-danger" onClick={handleDelete} disabled={busy}>
                  Delete invoice
                </button>
              )}
            </div>
          )}
        </section>
      </div>
    </main>
  )
}

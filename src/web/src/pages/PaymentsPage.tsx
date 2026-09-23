import { useEffect, useState } from 'react'
import type { ClientSummary } from '../api/clientsApi'
import { listClients } from '../api/clientsApi'
import { errorMessage } from '../api/http'
import type { Invoice } from '../api/invoicesApi'
import { InvoiceStatus, balanceDue, getInvoice, listInvoices } from '../api/invoicesApi'
import type { LedgerPayment, PaymentList } from '../api/paymentsApi'
import { PaymentMethod, createPayment, deletePayment, listPayments, paymentMethodLabels, updatePayment } from '../api/paymentsApi'
import { BackLink } from '../components/BackLink'
import { ErrorAlert } from '../components/ErrorAlert'
import { LoadingRows } from '../components/LoadingRows'
import { Pager } from '../components/Pager'
import type { InvoiceOption, PaymentFormValues } from '../components/PaymentForm'
import { PaymentForm } from '../components/PaymentForm'
import { clientName } from '../lib/clients'
import { formatDate, formatInvoiceNumber, formatMoney, fromDateInput, roundMoney, todayDateInput, toDateInput } from '../lib/format'
import type { Route } from '../lib/route'
import { navigate } from '../lib/route'

type PaymentsRoute = Extract<Route, { page: 'payments' }>

const PAGE_SIZE = 25

export function PaymentsPage({ route }: { route: PaymentsRoute }) {
  return route.view === 'new' ? <RecordPayment key={route.invoiceId ?? 'new'} invoiceId={route.invoiceId} /> : <PaymentLedger />
}

function PaymentLedger() {
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PaymentList | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [reloadToken, setReloadToken] = useState(0)
  const [editing, setEditing] = useState<{ payment: LedgerPayment; maxAmount: number } | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    let cancelled = false
    setError(null)
    listPayments({ page, pageSize: PAGE_SIZE })
      .then((result) => !cancelled && setData(result))
      .catch((err) => !cancelled && setError(errorMessage(err, 'Failed to load payments.')))
    return () => {
      cancelled = true
    }
  }, [page, reloadToken])

  const reload = () => setReloadToken((token) => token + 1)

  async function startEdit(payment: LedgerPayment) {
    setError(null)
    setFormError(null)
    try {
      // The ledger row doesn't carry the invoice balance, which bounds how much this payment may be.
      const invoice = await getInvoice(payment.invoiceId)
      setEditing({ payment, maxAmount: roundMoney(balanceDue(invoice) + payment.amount) })
    } catch (err) {
      setError(errorMessage(err, 'Failed to load the payment’s invoice.'))
    }
  }

  async function handleUpdate(values: PaymentFormValues) {
    if (!editing) {
      return
    }
    setBusy(true)
    setFormError(null)
    try {
      await updatePayment(editing.payment.invoiceId, editing.payment.id, {
        amount: Number(values.amount),
        paymentDate: fromDateInput(values.paymentDate),
        method: values.method,
        notes: values.notes || null,
        version: editing.payment.version,
      })
      setEditing(null)
      reload()
    } catch (err) {
      setFormError(errorMessage(err, 'Failed to save the payment.'))
    } finally {
      setBusy(false)
    }
  }

  async function handleDelete(payment: LedgerPayment) {
    const amount = formatMoney(payment.amount, payment.currency)
    if (!window.confirm(`Delete the ${amount} payment from ${payment.clientName}? The invoice will show as unpaid again.`)) {
      return
    }
    setError(null)
    try {
      await deletePayment(payment.invoiceId, payment.id)
      reload()
    } catch (err) {
      setError(errorMessage(err, 'Failed to delete the payment.'))
    }
  }

  return (
    <main>
      <header className="page-header">
        <div>
          <h1>Payments</h1>
          {data && data.totalRecords > 0 && (
            <p className="page-intro">{data.totalRecords === 1 ? '1 payment received' : `${data.totalRecords} payments received`}</p>
          )}
        </div>
        <a className="button-primary" href="#/payments/new">
          Record payment
        </a>
      </header>

      {error && <ErrorAlert message={error} onRetry={reload} />}

      {editing && (
        <PaymentForm
          key={editing.payment.id}
          heading={`Edit payment on invoice ${formatInvoiceNumber(editing.payment.invoiceNumber)}`}
          currency={editing.payment.currency}
          maxAmount={editing.maxAmount}
          initialValues={{
            invoiceId: editing.payment.invoiceId,
            amount: String(editing.payment.amount),
            paymentDate: toDateInput(editing.payment.paymentDate),
            method: editing.payment.method,
            notes: editing.payment.notes ?? '',
          }}
          submitLabel="Save payment"
          submitting={busy}
          error={formError}
          onSubmit={handleUpdate}
          onCancel={() => setEditing(null)}
        />
      )}

      {!data && !error && <LoadingRows label="Loading payments…" />}

      {data && data.totalRecords === 0 && (
        <div className="panel empty-state">
          <h2>No payments yet</h2>
          <p>When a client pays, record it here or from the invoice itself.</p>
          <a className="button-primary" href="#/payments/new">
            Record a payment
          </a>
        </div>
      )}

      {data && data.items.length > 0 && (
        <div className="panel panel-flush">
          <table className="data-table data-table-cards">
            <thead>
              <tr>
                <th scope="col">Date</th>
                <th scope="col">Client</th>
                <th scope="col">Invoice</th>
                <th scope="col">Method</th>
                <th scope="col" className="numeric">
                  Amount
                </th>
                <th scope="col">
                  <span className="visually-hidden">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((payment) => {
                const amount = formatMoney(payment.amount, payment.currency)
                return (
                  <tr key={payment.id}>
                    <td className="data-table-muted">{formatDate(payment.paymentDate)}</td>
                    <td className="data-table-primary">{payment.clientName}</td>
                    <td>
                      <a className="row-link" href={`#/invoices/${payment.invoiceId}`}>
                        {formatInvoiceNumber(payment.invoiceNumber)}
                      </a>
                    </td>
                    <td className="data-table-muted">
                      {paymentMethodLabels[payment.method]}
                      {payment.notes ? ` · ${payment.notes}` : ''}
                    </td>
                    <td className="numeric data-table-primary">{amount}</td>
                    <td className="data-table-actions">
                      <button
                        type="button"
                        className="button-ghost"
                        onClick={() => startEdit(payment)}
                        aria-label={`Edit ${amount} payment from ${payment.clientName}`}
                      >
                        Edit
                      </button>
                      <button
                        type="button"
                        className="button-ghost button-ghost-danger"
                        onClick={() => handleDelete(payment)}
                        aria-label={`Delete ${amount} payment from ${payment.clientName}`}
                      >
                        Delete
                      </button>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {data && <Pager page={page} totalPages={data.totalPages} onPageChange={setPage} />}
    </main>
  )
}

function RecordPayment({ invoiceId }: { invoiceId?: string }) {
  const [options, setOptions] = useState<InvoiceOption[] | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [selected, setSelected] = useState<Invoice | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  // Remounts the form once the chosen invoice has loaded, so the amount starts at its balance.
  const [formKey, setFormKey] = useState(0)

  useEffect(() => {
    // Only sent (or overdue) invoices can take a payment; drafts and void ones are rejected by the API.
    Promise.all([
      listInvoices({ status: InvoiceStatus.Sent, pageSize: 200 }),
      listInvoices({ status: InvoiceStatus.Overdue, pageSize: 200 }),
      listClients().catch((): ClientSummary[] | null => null),
    ])
      .then(([sent, overdue, clients]) => {
        const open = [...sent.items, ...overdue.items].sort((a, b) => a.dueDate.localeCompare(b.dueDate))
        setOptions(
          open.map((invoice) => ({
            id: invoice.id,
            label: `${formatInvoiceNumber(invoice.invoiceNumber)} · ${clientName(clients, invoice.clientId) || 'Client'} · ${formatMoney(invoice.grandTotal, invoice.currency)} · due ${formatDate(invoice.dueDate)}`,
          })),
        )
      })
      .catch((err) => setLoadError(errorMessage(err, 'Failed to load unpaid invoices.')))
  }, [])

  useEffect(() => {
    if (invoiceId) {
      selectInvoice(invoiceId)
    }
  }, [invoiceId])

  async function selectInvoice(id: string) {
    setSelected(null)
    if (!id) {
      return
    }
    try {
      setSelected(await getInvoice(id))
      setFormKey((key) => key + 1)
    } catch (err) {
      setError(errorMessage(err, 'Failed to load the invoice.'))
    }
  }

  async function handleSubmit(values: PaymentFormValues) {
    setSubmitting(true)
    setError(null)
    try {
      await createPayment(values.invoiceId, {
        amount: Number(values.amount),
        paymentDate: fromDateInput(values.paymentDate),
        method: values.method,
        notes: values.notes || null,
      })
      navigate('/payments')
    } catch (err) {
      setError(errorMessage(err, 'Failed to record the payment.'))
      setSubmitting(false)
    }
  }

  const balance = selected ? balanceDue(selected) : null

  return (
    <main className="page-narrow">
      <BackLink label="Back to payments" onClick={() => navigate('/payments')} disabled={submitting} />
      <header className="page-header">
        <div>
          <h1>Record payment</h1>
          <p className="page-intro">Log money received against an invoice you’ve sent.</p>
        </div>
      </header>

      {loadError && <ErrorAlert message={loadError} />}
      {!options && !loadError && <LoadingRows label="Loading unpaid invoices…" />}

      {options && options.length === 0 && (
        <div className="panel empty-state">
          <h2>Nothing to collect</h2>
          <p>Payments go against sent invoices, and none are waiting to be paid.</p>
          <a className="button-primary" href="#/invoices">
            Go to invoices
          </a>
        </div>
      )}

      {options && options.length > 0 && (
        <PaymentForm
          key={formKey}
          heading="Payment details"
          invoiceOptions={options}
          onInvoiceChange={selectInvoice}
          currency={selected?.currency ?? null}
          maxAmount={balance}
          initialValues={{
            invoiceId: selected?.id ?? '',
            amount: balance !== null ? balance.toFixed(2) : '',
            paymentDate: todayDateInput(),
            method: PaymentMethod.BankTransfer,
            notes: '',
          }}
          submitLabel="Record payment"
          submitting={submitting}
          error={error}
          onSubmit={handleSubmit}
          onCancel={() => navigate('/payments')}
        />
      )}
    </main>
  )
}

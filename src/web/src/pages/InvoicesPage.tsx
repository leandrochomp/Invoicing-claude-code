import { useEffect, useState } from 'react'
import type { ClientSummary } from '../api/clientsApi'
import { getClient, listClients } from '../api/clientsApi'
import { errorMessage } from '../api/http'
import type { Invoice, InvoiceList } from '../api/invoicesApi'
import { InvoiceStatus, createInvoice, getInvoice, invoiceStatusLabels, listInvoices, toUpdateInput, updateInvoice } from '../api/invoicesApi'
import { ErrorAlert } from '../components/ErrorAlert'
import type { InvoiceFormValues } from '../lib/invoiceLines'
import { InvoiceForm } from '../components/InvoiceForm'
import { invoiceToFormValues, newLine, toInvoiceInput } from '../lib/invoiceLines'
import { LoadingRows } from '../components/LoadingRows'
import { Pager } from '../components/Pager'
import { StatusBadge } from '../components/StatusBadge'
import { addDays, formatDate, formatInvoiceNumber, formatMoney, todayDateInput } from '../lib/format'
import { clientName } from '../lib/clients'
import type { Route } from '../lib/route'
import { navigate } from '../lib/route'
import { InvoiceDetailPage } from './InvoiceDetailPage'

type InvoicesRoute = Extract<Route, { page: 'invoices' }>

const PAGE_SIZE = 25

export function InvoicesPage({ route }: { route: InvoicesRoute }) {
  const [clients, setClients] = useState<ClientSummary[] | null>(null)
  const [clientsError, setClientsError] = useState<string | null>(null)

  useEffect(() => {
    listClients()
      .then(setClients)
      .catch((err) => setClientsError(errorMessage(err, 'Failed to load clients.')))
  }, [])

  if (clientsError && route.view !== 'list') {
    return <ErrorAlert message={clientsError} />
  }

  switch (route.view) {
    case 'new':
      return clients ? <CreateInvoice clients={clients} /> : <LoadingRows label="Loading clients…" />
    case 'edit':
      return clients ? <EditInvoice key={route.id} id={route.id} clients={clients} /> : <LoadingRows label="Loading invoice…" />
    case 'detail':
      return <InvoiceDetailPage key={route.id} id={route.id} clients={clients} />
    default:
      return <InvoiceListView clients={clients} />
  }
}

const statusFilters: { label: string; value: InvoiceStatus | undefined }[] = [
  { label: 'All', value: undefined },
  ...[InvoiceStatus.Draft, InvoiceStatus.Sent, InvoiceStatus.Overdue, InvoiceStatus.Paid, InvoiceStatus.Void].map((value) => ({
    label: invoiceStatusLabels[value],
    value,
  })),
]

function InvoiceListView({ clients }: { clients: ClientSummary[] | null }) {
  const [status, setStatus] = useState<InvoiceStatus | undefined>(undefined)
  const [page, setPage] = useState(1)
  const [data, setData] = useState<InvoiceList | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [reloadToken, setReloadToken] = useState(0)

  useEffect(() => {
    let cancelled = false
    setData(null)
    setError(null)
    listInvoices({ status, page, pageSize: PAGE_SIZE })
      .then((result) => !cancelled && setData(result))
      .catch((err) => !cancelled && setError(errorMessage(err, 'Failed to load invoices.')))
    return () => {
      cancelled = true
    }
  }, [status, page, reloadToken])

  function chooseStatus(value: InvoiceStatus | undefined) {
    setStatus(value)
    setPage(1)
  }

  const hasNoInvoicesAtAll = data && data.totalRecords === 0 && status === undefined

  return (
    <main>
      <header className="page-header">
        <div>
          <h1>Invoices</h1>
          {data && data.totalRecords > 0 && (
            <p className="page-intro">{data.totalRecords === 1 ? '1 invoice' : `${data.totalRecords} invoices`}</p>
          )}
        </div>
        <a className="button-primary" href="#/invoices/new">
          New invoice
        </a>
      </header>

      <div className="filter-bar" role="group" aria-label="Filter by status">
        {statusFilters.map((filter) => (
          <button
            key={filter.label}
            type="button"
            className="filter-chip"
            aria-pressed={status === filter.value}
            onClick={() => chooseStatus(filter.value)}
          >
            {filter.label}
          </button>
        ))}
      </div>

      {error && <ErrorAlert message={error} onRetry={() => setReloadToken((token) => token + 1)} />}

      {!data && !error && <LoadingRows label="Loading invoices…" />}

      {hasNoInvoicesAtAll && (
        <div className="panel empty-state">
          <h2>No invoices yet</h2>
          <p>Create a draft, check it over, then mark it as sent.</p>
          <a className="button-primary" href="#/invoices/new">
            Create your first invoice
          </a>
        </div>
      )}

      {data && data.totalRecords === 0 && status !== undefined && (
        <div className="panel empty-state">
          <h2>No {invoiceStatusLabels[status].toLowerCase()} invoices</h2>
          <p>Try another status, or show all invoices.</p>
          <button type="button" className="button-secondary" onClick={() => chooseStatus(undefined)}>
            Show all invoices
          </button>
        </div>
      )}

      {data && data.items.length > 0 && (
        <div className="panel panel-flush">
          <table className="data-table data-table-cards">
            <thead>
              <tr>
                <th scope="col">Invoice</th>
                <th scope="col">Client</th>
                <th scope="col">Due</th>
                <th scope="col">Status</th>
                <th scope="col" className="numeric">
                  Total
                </th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((invoice) => (
                <tr key={invoice.id}>
                  <td className="data-table-primary">
                    <a className="row-link" href={`#/invoices/${invoice.id}`}>
                      {formatInvoiceNumber(invoice.invoiceNumber)}
                    </a>
                  </td>
                  <td>{clientName(clients, invoice.clientId)}</td>
                  <td className="data-table-muted">{formatDate(invoice.dueDate)}</td>
                  <td>
                    <StatusBadge status={invoice.status} dueDate={invoice.dueDate} />
                  </td>
                  <td className="numeric">{formatMoney(invoice.grandTotal, invoice.currency)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {data && <Pager page={page} totalPages={data.totalPages} onPageChange={setPage} />}
    </main>
  )
}

async function preferredCurrency(clientId: string): Promise<string | null> {
  try {
    return (await getClient(clientId)).preferredCurrency
  } catch {
    return null
  }
}

function CreateInvoice({ clients }: { clients: ClientSummary[] }) {
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [initialValues] = useState<InvoiceFormValues>(() => {
    const today = todayDateInput()
    return { clientId: '', issueDate: today, dueDate: addDays(today, 30), currency: '', notes: '', items: [newLine()] }
  })

  if (clients.length === 0) {
    return (
      <main className="panel empty-state">
        <h1>Add a client first</h1>
        <p>Every invoice is billed to a client.</p>
        <a className="button-primary" href="#/clients">
          Go to clients
        </a>
      </main>
    )
  }

  async function handleSubmit(values: InvoiceFormValues) {
    setSubmitting(true)
    setError(null)
    try {
      const created = await createInvoice(toInvoiceInput(values))
      navigate(`/invoices/${created.id}`)
    } catch (err) {
      setError(errorMessage(err, 'Failed to create the invoice.'))
      setSubmitting(false)
    }
  }

  return (
    <InvoiceForm
      mode="create"
      title="New invoice"
      clients={clients}
      initialValues={initialValues}
      submitting={submitting}
      error={error}
      onClientChange={preferredCurrency}
      onSubmit={handleSubmit}
      onCancel={() => navigate('/invoices')}
    />
  )
}

function EditInvoice({ id, clients }: { id: string; clients: ClientSummary[] }) {
  const [invoice, setInvoice] = useState<Invoice | null>(null)
  const [initialValues, setInitialValues] = useState<InvoiceFormValues | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    getInvoice(id)
      .then((loaded) => {
        setInvoice(loaded)
        setInitialValues(invoiceToFormValues(loaded))
      })
      .catch((err) => setLoadError(errorMessage(err, 'Failed to load the invoice.')))
  }, [id])

  if (loadError) {
    return <ErrorAlert message={loadError} />
  }
  if (!invoice || !initialValues) {
    return <LoadingRows label="Loading invoice…" />
  }

  async function handleSubmit(values: InvoiceFormValues) {
    if (!invoice) {
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      await updateInvoice(id, toUpdateInput(invoice, toInvoiceInput(values)))
      navigate(`/invoices/${id}`)
    } catch (err) {
      setError(errorMessage(err, 'Failed to save the invoice.'))
      setSubmitting(false)
    }
  }

  return (
    <InvoiceForm
      mode="edit"
      title={`Edit invoice ${formatInvoiceNumber(invoice.invoiceNumber)}`}
      clients={clients}
      initialValues={initialValues}
      submitting={submitting}
      error={error}
      onSubmit={handleSubmit}
      onCancel={() => navigate(`/invoices/${id}`)}
    />
  )
}

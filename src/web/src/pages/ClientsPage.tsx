import { useEffect, useState } from 'react'
import type { ClientInput, ClientSummary, UpdateClientInput } from '../api/clientsApi'
import { ClientApiError, createClient, deleteClient, getClient, listClients, updateClient } from '../api/clientsApi'
import type { ClientFormValues } from '../components/ClientForm'
import { ClientForm } from '../components/ClientForm'

type View = { mode: 'list' } | { mode: 'create' } | { mode: 'edit'; id: string; values: ClientFormValues }

function toInput(values: ClientFormValues): ClientInput {
  return {
    companyName: values.companyName,
    contactName: values.contactName || null,
    email: values.email,
    phone: values.phone || null,
    addressLine1: values.addressLine1,
    addressLine2: values.addressLine2 || null,
    city: values.city,
    stateOrRegion: values.stateOrRegion,
    postalCode: values.postalCode,
    country: values.country,
    preferredCurrency: values.preferredCurrency,
  }
}

export function ClientsPage() {
  const [clients, setClients] = useState<ClientSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [listError, setListError] = useState<string | null>(null)
  const [view, setView] = useState<View>({ mode: 'list' })
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  async function refresh() {
    try {
      const data = await listClients()
      setClients(data)
      setListError(null)
    } catch (err) {
      setListError(err instanceof ClientApiError ? err.message : 'Failed to load clients.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    refresh()
  }, [])

  async function startEdit(id: string) {
    setListError(null)
    try {
      const detail = await getClient(id)
      setView({
        mode: 'edit',
        id,
        values: {
          companyName: detail.companyName,
          contactName: detail.contactName ?? '',
          email: detail.email,
          phone: detail.phone ?? '',
          addressLine1: detail.addressLine1,
          addressLine2: detail.addressLine2 ?? '',
          city: detail.city,
          stateOrRegion: detail.stateOrRegion,
          postalCode: detail.postalCode,
          country: detail.country,
          preferredCurrency: detail.preferredCurrency,
          isActive: detail.isActive,
        },
      })
    } catch (err) {
      setListError(err instanceof ClientApiError ? err.message : 'Failed to load client.')
    }
  }

  async function handleCreate(values: ClientFormValues) {
    setSubmitting(true)
    setFormError(null)
    try {
      await createClient(toInput(values))
      setView({ mode: 'list' })
      await refresh()
    } catch (err) {
      setFormError(err instanceof ClientApiError ? err.message : 'Failed to create client.')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleUpdate(id: string, values: ClientFormValues) {
    setSubmitting(true)
    setFormError(null)
    try {
      const input: UpdateClientInput = { ...toInput(values), isActive: values.isActive }
      await updateClient(id, input)
      setView({ mode: 'list' })
      await refresh()
    } catch (err) {
      setFormError(err instanceof ClientApiError ? err.message : 'Failed to update client.')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDelete(id: string, companyName: string) {
    if (!window.confirm(`Delete ${companyName}? It will be removed from your client list.`)) {
      return
    }
    setListError(null)
    try {
      await deleteClient(id)
      await refresh()
    } catch (err) {
      setListError(err instanceof ClientApiError ? err.message : 'Failed to delete client.')
    }
  }

  if (view.mode === 'create') {
    return (
      <ClientForm
        mode="create"
        submitting={submitting}
        error={formError}
        onSubmit={handleCreate}
        onCancel={() => setView({ mode: 'list' })}
      />
    )
  }

  if (view.mode === 'edit') {
    return (
      <ClientForm
        mode="edit"
        initialValues={view.values}
        submitting={submitting}
        error={formError}
        onSubmit={(values) => handleUpdate(view.id, values)}
        onCancel={() => setView({ mode: 'list' })}
      />
    )
  }

  function startCreate() {
    setFormError(null)
    setView({ mode: 'create' })
  }

  return (
    <main className="clients-page">
      <header className="page-header">
        <div>
          <h1>Clients</h1>
          {!loading && !listError && clients.length > 0 && (
            <p className="page-intro">
              {clients.length === 1 ? '1 client' : `${clients.length} clients`}
            </p>
          )}
        </div>
        <button type="button" className="button-primary" onClick={startCreate}>
          Add client
        </button>
      </header>

      {listError && (
        <div className="form-alert form-alert-row" role="alert">
          <span>{listError}</span>
          <button
            type="button"
            className="button-secondary button-small"
            onClick={() => {
              setLoading(true)
              refresh()
            }}
          >
            Try again
          </button>
        </div>
      )}

      {loading && (
        <div className="panel panel-flush" aria-busy="true">
          <p className="visually-hidden">Loading clients…</p>
          {[0, 1, 2].map((row) => (
            <div key={row} className="skeleton-row" aria-hidden="true">
              <span className="skeleton skeleton-wide" />
              <span className="skeleton" />
            </div>
          ))}
        </div>
      )}

      {!loading && !listError && clients.length === 0 && (
        <div className="panel empty-state">
          <h2>No clients yet</h2>
          <p>Add a client to start sending them invoices.</p>
          <button type="button" className="button-primary" onClick={startCreate}>
            Add your first client
          </button>
        </div>
      )}

      {!loading && clients.length > 0 && (
        <div className="panel panel-flush">
          <table className="data-table">
            <thead>
              <tr>
                <th scope="col">Company</th>
                <th scope="col">Email</th>
                <th scope="col">
                  <span className="visually-hidden">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {clients.map((clientRow) => (
                <tr key={clientRow.id}>
                  <td className="data-table-primary">{clientRow.companyName}</td>
                  <td className="data-table-muted">{clientRow.email}</td>
                  <td className="data-table-actions">
                    <button
                      type="button"
                      className="button-ghost"
                      onClick={() => startEdit(clientRow.id)}
                      aria-label={`Edit ${clientRow.companyName}`}
                    >
                      Edit
                    </button>
                    <button
                      type="button"
                      className="button-ghost button-ghost-danger"
                      onClick={() => handleDelete(clientRow.id, clientRow.companyName)}
                      aria-label={`Delete ${clientRow.companyName}`}
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </main>
  )
}

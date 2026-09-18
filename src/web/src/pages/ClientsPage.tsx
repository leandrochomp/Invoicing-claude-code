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

  async function handleDelete(id: string) {
    if (!window.confirm('Delete this client?')) {
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

  return (
    <section className="clients-page">
      <header className="clients-header">
        <h1>Clients</h1>
        <button
          type="button"
          onClick={() => {
            setFormError(null)
            setView({ mode: 'create' })
          }}
        >
          Add client
        </button>
      </header>

      {loading && <p>Loading clients…</p>}
      {listError && (
        <p className="clients-error" role="alert">
          {listError}
        </p>
      )}
      {!loading && !listError && clients.length === 0 && <p>No clients yet.</p>}

      {!loading && clients.length > 0 && (
        <table className="clients-table">
          <thead>
            <tr>
              <th>Company</th>
              <th>Email</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {clients.map((clientRow) => (
              <tr key={clientRow.id}>
                <td>{clientRow.companyName}</td>
                <td>{clientRow.email}</td>
                <td className="clients-row-actions">
                  <button type="button" onClick={() => startEdit(clientRow.id)}>
                    Edit
                  </button>
                  <button type="button" onClick={() => handleDelete(clientRow.id)}>
                    Delete
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  )
}

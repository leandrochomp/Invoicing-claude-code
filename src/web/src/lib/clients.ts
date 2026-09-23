import type { ClientSummary } from '../api/clientsApi'

// Clients are soft-deleted and drop out of the client list, but their invoices and payments remain.
export function clientName(clients: ClientSummary[] | null, clientId: string): string {
  if (!clients) {
    return ''
  }
  return clients.find((client) => client.id === clientId)?.companyName ?? 'Deleted client'
}

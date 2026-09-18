export interface ClientSummary {
  id: string
  companyName: string
  email: string
}

export interface ClientDetail {
  id: string
  companyName: string
  contactName: string | null
  email: string
  phone: string | null
  addressLine1: string
  addressLine2: string | null
  city: string
  stateOrRegion: string
  postalCode: string
  country: string
  preferredCurrency: string
  isActive: boolean
}

export interface ClientInput {
  companyName: string
  contactName: string | null
  email: string
  phone: string | null
  addressLine1: string
  addressLine2: string | null
  city: string
  stateOrRegion: string
  postalCode: string
  country: string
  preferredCurrency: string
}

export interface UpdateClientInput extends ClientInput {
  isActive: boolean
}

export class ClientApiError extends Error {}

async function parseErrorMessage(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as { title?: string; errors?: Record<string, string[]> }
    if (problem.errors) {
      return Object.values(problem.errors).flat().join(' ')
    }
    return problem.title ?? 'The request failed.'
  } catch {
    return 'The request failed.'
  }
}

async function requestJson<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    ...init,
  })

  if (!response.ok) {
    throw new ClientApiError(await parseErrorMessage(response))
  }

  return (await response.json()) as T
}

export function listClients(): Promise<ClientSummary[]> {
  return requestJson<ClientSummary[]>('/bff/clients')
}

export function getClient(id: string): Promise<ClientDetail> {
  return requestJson<ClientDetail>(`/bff/clients/${id}`)
}

export function createClient(input: ClientInput): Promise<ClientSummary> {
  return requestJson<ClientSummary>('/bff/clients', { method: 'POST', body: JSON.stringify(input) })
}

export function updateClient(id: string, input: UpdateClientInput): Promise<ClientSummary> {
  return requestJson<ClientSummary>(`/bff/clients/${id}`, { method: 'PUT', body: JSON.stringify(input) })
}

export async function deleteClient(id: string): Promise<void> {
  const response = await fetch(`/bff/clients/${id}`, { method: 'DELETE', credentials: 'include' })
  if (!response.ok) {
    throw new ClientApiError(await parseErrorMessage(response))
  }
}

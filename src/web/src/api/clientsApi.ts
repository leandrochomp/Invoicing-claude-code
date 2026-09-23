import { requestJson, requestNoContent } from './http'

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

export function deleteClient(id: string): Promise<void> {
  return requestNoContent(`/bff/clients/${id}`, { method: 'DELETE' })
}

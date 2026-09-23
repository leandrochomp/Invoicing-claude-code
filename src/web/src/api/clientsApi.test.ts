import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { ClientInput, UpdateClientInput } from './clientsApi'
import { createClient, deleteClient, getClient, listClients, updateClient } from './clientsApi'
import { ApiError } from './http'

const sampleInput: ClientInput = {
  companyName: 'Acme Corp',
  contactName: 'Jane Doe',
  email: 'billing@acme.test',
  phone: '555-0100',
  addressLine1: '1 Main St',
  addressLine2: null,
  city: 'Springfield',
  stateOrRegion: 'IL',
  postalCode: '62701',
  country: 'US',
  preferredCurrency: 'USD',
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn())
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('listClients', () => {
  it('fetches /bff/clients with cookies included', async () => {
    vi.mocked(fetch).mockResolvedValue(
      new Response(JSON.stringify([{ id: '1', companyName: 'Acme Corp', email: 'billing@acme.test' }]), { status: 200 }),
    )

    const clients = await listClients()

    expect(fetch).toHaveBeenCalledWith('/bff/clients', {
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
    })
    expect(clients).toEqual([{ id: '1', companyName: 'Acme Corp', email: 'billing@acme.test' }])
  })

  it('throws an ApiError with the server-provided title on failure', async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(JSON.stringify({ title: 'Unable to reach the Invoicing API.' }), { status: 503 }))

    await expect(listClients()).rejects.toThrow('Unable to reach the Invoicing API.')
  })

  it('joins validation errors into a single message', async () => {
    vi.mocked(fetch).mockResolvedValue(
      new Response(JSON.stringify({ errors: { Email: ['Email is required.'], CompanyName: ['CompanyName is required.'] } }), {
        status: 400,
      }),
    )

    const error = await listClients().catch((err) => err)

    expect(error).toBeInstanceOf(ApiError)
    expect(error.message).toBe('Email is required. CompanyName is required.')
  })
})

describe('getClient', () => {
  it('fetches /bff/clients/{id}', async () => {
    vi.mocked(fetch).mockResolvedValue(
      new Response(JSON.stringify({ id: '1', companyName: 'Acme Corp' }), { status: 200 }),
    )

    await getClient('1')

    expect(fetch).toHaveBeenCalledWith('/bff/clients/1', {
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
    })
  })
})

describe('createClient', () => {
  it('posts the client input as JSON', async () => {
    vi.mocked(fetch).mockResolvedValue(
      new Response(JSON.stringify({ id: '1', companyName: 'Acme Corp', email: 'billing@acme.test' }), { status: 201 }),
    )

    await createClient(sampleInput)

    expect(fetch).toHaveBeenCalledWith('/bff/clients', {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(sampleInput),
    })
  })
})

describe('updateClient', () => {
  it('puts the client input as JSON', async () => {
    const input: UpdateClientInput = { ...sampleInput, isActive: false }
    vi.mocked(fetch).mockResolvedValue(
      new Response(JSON.stringify({ id: '1', companyName: 'Acme Corp', email: 'billing@acme.test' }), { status: 200 }),
    )

    await updateClient('1', input)

    expect(fetch).toHaveBeenCalledWith('/bff/clients/1', {
      method: 'PUT',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(input),
    })
  })
})

describe('deleteClient', () => {
  it('sends a DELETE request with cookies included', async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(null, { status: 204 }))

    await deleteClient('1')

    expect(fetch).toHaveBeenCalledWith('/bff/clients/1', expect.objectContaining({ method: 'DELETE', credentials: 'include' }))
  })

  it('throws an ApiError on failure', async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(JSON.stringify({ title: 'Forbidden.' }), { status: 403 }))

    await expect(deleteClient('1')).rejects.toThrow('Forbidden.')
  })
})

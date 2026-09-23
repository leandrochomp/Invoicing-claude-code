// Shared fetch plumbing for the BFF resource modules. Every call sends the BFF session cookie and turns
// a ProblemDetails / ValidationProblem response into an ApiError carrying a readable message.

export class ApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.status = status
  }
}

async function parseErrorMessage(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as { title?: string; detail?: string; errors?: Record<string, string[]> }
    if (problem.errors) {
      return Object.values(problem.errors).flat().join(' ')
    }
    return problem.detail ?? problem.title ?? 'The request failed.'
  } catch {
    return 'The request failed.'
  }
}

async function send(url: string, init?: RequestInit): Promise<Response> {
  const response = await fetch(url, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    ...init,
  })

  if (!response.ok) {
    throw new ApiError(await parseErrorMessage(response), response.status)
  }
  return response
}

export async function requestJson<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await send(url, init)
  return (await response.json()) as T
}

export async function requestNoContent(url: string, init?: RequestInit): Promise<void> {
  await send(url, init)
}

export function errorMessage(err: unknown, fallback: string): string {
  return err instanceof ApiError ? err.message : fallback
}

export function withQuery(path: string, params: Record<string, string | number | null | undefined>): string {
  const query = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== null && value !== undefined && value !== '') {
      query.set(key, String(value))
    }
  }
  const text = query.toString()
  return text ? `${path}?${text}` : path
}

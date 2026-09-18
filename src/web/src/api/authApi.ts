export interface LoginRequest {
  username: string
  password: string
}

export interface Session {
  username: string
}

export class LoginError extends Error {}

async function parseErrorMessage(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as { title?: string }
    return problem.title ?? 'Login failed.'
  } catch {
    return 'Login failed.'
  }
}

export async function login(request: LoginRequest): Promise<void> {
  const response = await fetch('/bff/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new LoginError(await parseErrorMessage(response))
  }
}

export async function logout(): Promise<void> {
  await fetch('/bff/logout', { method: 'POST', credentials: 'include' })
}

export async function getSession(): Promise<Session | null> {
  const response = await fetch('/bff/session', { credentials: 'include' })
  if (response.status === 401) {
    return null
  }
  if (!response.ok) {
    throw new Error('Failed to load session.')
  }
  return (await response.json()) as Session
}

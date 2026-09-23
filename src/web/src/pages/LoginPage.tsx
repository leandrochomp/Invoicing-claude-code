import { useRef, useState } from 'react'
import type { FormEvent, KeyboardEvent } from 'react'
import { login, LoginError } from '../api/authApi'
import { Field } from '../components/Field'

interface LoginPageProps {
  onLoggedIn: () => void
}

interface LoginErrors {
  username?: string
  password?: string
}

export function LoginPage({ onLoggedIn }: LoginPageProps) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [capsLockOn, setCapsLockOn] = useState(false)
  const [fieldErrors, setFieldErrors] = useState<LoginErrors>({})
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const usernameRef = useRef<HTMLInputElement>(null)
  const passwordRef = useRef<HTMLInputElement>(null)

  function handleKeyEvent(event: KeyboardEvent<HTMLInputElement>) {
    setCapsLockOn(event.getModifierState('CapsLock'))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const nextErrors: LoginErrors = {
      username: username.trim() === '' ? 'Enter your username.' : undefined,
      password: password === '' ? 'Enter your password.' : undefined,
    }
    setFieldErrors(nextErrors)
    if (nextErrors.username || nextErrors.password) {
      ;(nextErrors.username ? usernameRef : passwordRef).current?.focus()
      return
    }

    setError(null)
    setSubmitting(true)

    try {
      await login({ username, password })
      onLoggedIn()
    } catch (err) {
      setError(err instanceof LoginError ? err.message : 'Something went wrong. Please try again.')
      // Keep the username so a mistyped password is a quick retry.
      setPassword('')
      passwordRef.current?.focus()
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="login-page">
      <form className="login-card" onSubmit={handleSubmit} noValidate aria-labelledby="login-title">
        <p className="brand">Invoicing</p>
        <h1 id="login-title">Sign in</h1>

        {error && (
          <div className="form-alert" role="alert">
            {error}
          </div>
        )}

        <Field id="username" label="Username" error={fieldErrors.username}>
          {(describedBy) => (
            <input
              ref={usernameRef}
              id="username"
              name="username"
              type="text"
              autoComplete="username"
              autoCapitalize="none"
              spellCheck={false}
              value={username}
              onChange={(event) => {
                setUsername(event.target.value)
                setFieldErrors((current) => ({ ...current, username: undefined }))
              }}
              required
              aria-invalid={fieldErrors.username ? true : undefined}
              aria-describedby={describedBy}
              autoFocus
            />
          )}
        </Field>

        <Field
          id="password"
          label="Password"
          hint={capsLockOn ? 'Caps Lock is on.' : undefined}
          error={fieldErrors.password}
        >
          {(describedBy) => (
            <div className="input-with-action">
              <input
                ref={passwordRef}
                id="password"
                name="password"
                type={showPassword ? 'text' : 'password'}
                autoComplete="current-password"
                value={password}
                onChange={(event) => {
                  setPassword(event.target.value)
                  setFieldErrors((current) => ({ ...current, password: undefined }))
                }}
                onKeyDown={handleKeyEvent}
                onKeyUp={handleKeyEvent}
                required
                aria-invalid={fieldErrors.password ? true : undefined}
                aria-describedby={describedBy}
              />
              <button
                type="button"
                className="input-action"
                onClick={() => setShowPassword((current) => !current)}
                aria-controls="password"
                aria-label={showPassword ? 'Hide password' : 'Show password'}
              >
                {showPassword ? 'Hide' : 'Show'}
              </button>
            </div>
          )}
        </Field>

        <button type="submit" className="button-primary button-block" disabled={submitting}>
          {submitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </main>
  )
}

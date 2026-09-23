import type { ReactNode } from 'react'

interface FieldProps {
  id: string
  label: string
  optional?: boolean
  hint?: string
  error?: string
  wide?: boolean
  // Receives the aria-describedby value so the input announces its hint and error.
  children: (describedBy: string | undefined) => ReactNode
}

// Label, control, hint and inline error for a single form field. Shared by every form so they
// look and behave the same.
export function Field({ id, label, optional, hint, error, wide, children }: FieldProps) {
  const hintId = `${id}-hint`
  const errorId = `${id}-error`
  const describedBy = [hint ? hintId : null, error ? errorId : null].filter(Boolean).join(' ') || undefined

  return (
    <div className={wide ? 'field field-wide' : 'field'}>
      <label htmlFor={id}>
        {label}
        {optional && <span className="field-optional"> (optional)</span>}
      </label>
      {children(describedBy)}
      {hint && (
        <p id={hintId} className="field-hint">
          {hint}
        </p>
      )}
      {error && (
        <p id={errorId} className="field-error">
          {error}
        </p>
      )}
    </div>
  )
}

import { useEffect, useRef, useState } from 'react'
import { DayPicker } from 'react-day-picker'
import { enAU } from 'react-day-picker/locale'
import 'react-day-picker/style.css'
import { formatDateField, parseDateField } from '../lib/format'

interface DatePickerProps {
  id: string
  // Calendar day as YYYY-MM-DD, or '' when empty or not a valid date.
  value: string
  onChange: (value: string) => void
  min?: string
  required?: boolean
  invalid?: boolean
  describedBy?: string
}

// DayPicker works with local Date objects; build them from the parts so no time zone shifts the day.
function toLocalDate(value: string): Date | undefined {
  if (!value) {
    return undefined
  }
  const [year, month, day] = value.split('-').map(Number)
  return new Date(year, month - 1, day)
}

function fromLocalDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

// Text field that takes Australian DD/MM/YYYY dates, with a calendar popover for picking one instead.
// Replaces <input type="date">, whose format follows the browser's locale (often US month-first).
export function DatePicker({ id, value, onChange, min, required, invalid, describedBy }: DatePickerProps) {
  const [text, setText] = useState(() => formatDateField(value))
  const [shownValue, setShownValue] = useState(value)
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)
  const toggleRef = useRef<HTMLButtonElement>(null)

  // The parent changed the value (e.g. a reset); a half-typed date parses to '' and must not wipe the text.
  if (value !== shownValue) {
    setShownValue(value)
    if (value !== parseDateField(text)) {
      setText(formatDateField(value))
    }
  }

  useEffect(() => {
    if (!open) {
      return
    }
    function closeOnOutsideClick(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', closeOnOutsideClick)
    return () => document.removeEventListener('mousedown', closeOnOutsideClick)
  }, [open])

  function close() {
    setOpen(false)
    toggleRef.current?.focus()
  }

  const selected = toLocalDate(value)
  const minDate = toLocalDate(min ?? '')

  return (
    <div className="date-picker" ref={rootRef}>
      <div className="date-picker-control">
        <input
          id={id}
          type="text"
          inputMode="numeric"
          placeholder="DD/MM/YYYY"
          autoComplete="off"
          value={text}
          onChange={(event) => {
            setText(event.target.value)
            onChange(parseDateField(event.target.value))
          }}
          onBlur={() => value && setText(formatDateField(value))}
          required={required}
          aria-invalid={invalid ? true : undefined}
          aria-describedby={describedBy}
        />
        <button
          ref={toggleRef}
          type="button"
          className="date-picker-toggle"
          aria-label="Choose date"
          aria-haspopup="dialog"
          aria-expanded={open}
          aria-controls={open ? `${id}-calendar` : undefined}
          onClick={() => setOpen((current) => !current)}
        >
          <svg viewBox="0 0 20 20" width="18" height="18" aria-hidden="true">
            <rect x="3" y="4.5" width="14" height="12.5" rx="2" fill="none" stroke="currentColor" strokeWidth="1.5" />
            <path d="M3 8.5h14M7 2.5v4M13 2.5v4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
          </svg>
        </button>
      </div>
      {open && (
        <div
          id={`${id}-calendar`}
          className="date-picker-popover"
          role="dialog"
          aria-label="Calendar"
          onKeyDown={(event) => {
            if (event.key === 'Escape') {
              event.stopPropagation()
              close()
            }
          }}
        >
          <DayPicker
            mode="single"
            locale={enAU}
            autoFocus
            selected={selected}
            defaultMonth={selected ?? minDate}
            disabled={minDate ? { before: minDate } : undefined}
            onSelect={(date) => {
              if (date) {
                onChange(fromLocalDate(date))
                setText(formatDateField(fromLocalDate(date)))
              }
              close()
            }}
          />
        </div>
      )}
    </div>
  )
}

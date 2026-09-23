interface BackLinkProps {
  label: string
  onClick: () => void
  disabled?: boolean
}

export function BackLink({ label, onClick, disabled }: BackLinkProps) {
  return (
    <button type="button" className="back-link" onClick={onClick} disabled={disabled}>
      <svg viewBox="0 0 16 16" width="16" height="16" aria-hidden="true">
        <path d="M10 3 5 8l5 5" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
      {label}
    </button>
  )
}

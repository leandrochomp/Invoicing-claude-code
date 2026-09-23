export function ErrorAlert({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="form-alert form-alert-row" role="alert">
      <span>{message}</span>
      {onRetry && (
        <button type="button" className="button-secondary button-small" onClick={onRetry}>
          Try again
        </button>
      )}
    </div>
  )
}

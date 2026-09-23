interface PagerProps {
  page: number
  totalPages: number
  onPageChange: (page: number) => void
}

export function Pager({ page, totalPages, onPageChange }: PagerProps) {
  if (totalPages <= 1) {
    return null
  }
  return (
    <nav className="pager" aria-label="Pagination">
      <button type="button" className="button-secondary button-small" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>
        Previous
      </button>
      <span>
        Page {page} of {totalPages}
      </span>
      <button
        type="button"
        className="button-secondary button-small"
        disabled={page >= totalPages}
        onClick={() => onPageChange(page + 1)}
      >
        Next
      </button>
    </nav>
  )
}

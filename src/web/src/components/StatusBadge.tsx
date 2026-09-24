import type { InvoiceStatus } from '../api/invoicesApi'
import { invoiceStatusLabels } from '../api/invoicesApi'

// The API already reports a Sent invoice past its due date as Overdue, so the badge shows the status as given.
export function StatusBadge({ status }: { status: InvoiceStatus }) {
  const label = invoiceStatusLabels[status]
  return <span className={`badge badge-${label.toLowerCase()}`}>{label}</span>
}

import { InvoiceStatus, invoiceStatusLabels } from '../api/invoicesApi'
import { todayDateInput, toDateInput } from '../lib/format'

// Nothing moves a Sent invoice to Overdue on the server, so a Sent invoice past its due date is shown
// as overdue here — the same rule the dashboard uses.
function displayStatus(status: InvoiceStatus, dueDate: string): InvoiceStatus {
  return status === InvoiceStatus.Sent && toDateInput(dueDate) < todayDateInput() ? InvoiceStatus.Overdue : status
}

export function StatusBadge({ status, dueDate }: { status: InvoiceStatus; dueDate: string }) {
  const shown = displayStatus(status, dueDate)
  const label = invoiceStatusLabels[shown]
  return <span className={`badge badge-${label.toLowerCase()}`}>{label}</span>
}

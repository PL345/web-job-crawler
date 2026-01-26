import { memo } from 'react'
import '../../styles/components.css'

/**
 * Status badge with color coding
 * Note: Memoized - status rarely changes, prevents color recalculation
 */
export const StatusBadge = memo(({ status, className = '' }) => {
  const statusColors = {
    'Pending': '#666',
    'Running': '#2563eb',
    'Completed': '#059669',
    'Failed': '#dc2626',
    'Cancelled': '#666'
  }

  return (
    <span
      className={`status-badge ${className}`}
      style={{ color: statusColors[status] || '#666' }}
    >
      {status}
    </span>
  )
})

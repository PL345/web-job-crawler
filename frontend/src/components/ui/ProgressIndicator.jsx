import { memo } from 'react'
import '../../styles/components.css'

/**
 * Progress indicator component
 * Note: Memoized - prevents percentage recalculation on parent re-renders
 */
export const ProgressIndicator = memo(({ current, total, label = '' }) => {
  const percentage = total > 0 ? (current / total * 100) : 0
  return (
    <div className="progress-container">
      <div className="progress-bar">
        <div className="progress-fill" style={{ width: `${percentage}%` }} />
      </div>
      {label && <span className="progress-label">{label}</span>}
      <span className="progress-text">{current} / {total}</span>
    </div>
  )
})

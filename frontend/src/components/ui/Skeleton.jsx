import { memo } from 'react'
import '../../styles/components.css'

/**
 * Skeleton loader placeholder
 * Note: Memoized - props stable during loading phase
 */
export const Skeleton = memo(({ width = '100%', height = '160px', className = '' }) => (
  <div
    className={`skeleton ${className}`}
    style={{ width, height }}
  />
))

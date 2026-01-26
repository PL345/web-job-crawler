import '../../styles/components.css'

/**
 * Reusable Card component for content containers
 * Note: No memo - simple wrapper, very cheap to render
 */
export const Card = ({ children, className = '', ...props }) => (
  <div className={`card ${className}`} {...props}>
    {children}
  </div>
)

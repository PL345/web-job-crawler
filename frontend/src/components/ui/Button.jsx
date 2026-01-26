import '../../styles/components.css'

/**
 * Reusable Button component with variants
 * Note: No memo - props change frequently (onClick, loading, disabled)
 */
export const Button = ({
  children,
  variant = 'primary',
  size = 'md',
  disabled = false,
  loading = false,
  type = 'button',
  className = '',
  ...props
}) => {
  const baseClass = `btn btn-${variant} btn-${size}`
  return (
    <button
      type={type}
      disabled={disabled || loading}
      className={`${baseClass} ${className}`}
      {...props}
    >
      {loading ? '...' : children}
    </button>
  )
}

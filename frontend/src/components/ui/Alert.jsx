import '../../styles/components.css'

/**
 * Alert/Toast component for notifications
 * Note: No memo - message changes frequently, onClose is new function each render
 */
export const Alert = ({ type = 'info', message, onClose = () => {}, dismissible = true }) => {
  const alertClass = `alert alert-${type}`
  return (
    <div className={alertClass}>
      <div className="alert-content">{message}</div>
      {dismissible && (
        <button className="alert-close" onClick={onClose}>✕</button>
      )}
    </div>
  )
}

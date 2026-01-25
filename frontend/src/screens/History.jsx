import { useState, useEffect } from 'react'
import { API_BASE_URL } from '../config'
import '../styles/screens.css'

export default function History({ onSelectJob }) {
  const [jobs, setJobs] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(0)
  const [isUpdating, setIsUpdating] = useState(false)

  useEffect(() => {
    const fetchJobs = async (isInitialLoad = false) => {
      if (isInitialLoad) setLoading(true)
      else setIsUpdating(true)
      
      try {
        const response = await fetch(`${API_BASE_URL}/jobs/history?page=${page}&pageSize=10`)
        if (!response.ok) throw new Error('Failed to fetch history')
        const data = await response.json()
        setJobs(data.jobs)
        setTotalPages(data.totalPages)
      } catch (err) {
        setError(err.message)
      } finally {
        if (isInitialLoad) setLoading(false)
        else setIsUpdating(false)
      }
    }

    // Initial load with loading state
    fetchJobs(true)
    
    // Poll for updates every 3 seconds WITHOUT loading state
    const interval = setInterval(() => fetchJobs(false), 3000)
    return () => clearInterval(interval)
  }, [page])

  const formatJobStatus = (job) => {
    if (job.status === 'Running') {
      return (
        <div className="running-status">
          <span className="status-badge" style={{ color: statusColor[job.status] }}>
            {job.status}
          </span>
          <div className="progress-info">
            <small>{job.pagesProcessed || 0} pages processed</small>
            {job.currentUrl && (
              <small className="current-url" title={job.currentUrl}>
                Currently: {job.currentUrl.length > 40 ? job.currentUrl.substring(0, 40) + '...' : job.currentUrl}
              </small>
            )}
          </div>
        </div>
      )
    }
    return (
      <span className="status-badge" style={{ color: statusColor[job.status] }}>
        {job.status}
      </span>
    )
  }

  const statusColor = {
    'Pending': '#666',
    'Running': '#2563eb',
    'Completed': '#059669',
    'Failed': '#dc2626',
    'Canceled': '#666'
  }

  if (loading) {
    return (
      <div className="screen history">
        <div className="loading">Loading history...</div>
      </div>
    )
  }

  return (
    <div className="screen history">
      <div className="history-header">
        <h2>Crawl History</h2>
        {isUpdating && <div className="update-indicator">●</div>}
      </div>

      {error && <div className="error-message">{error}</div>}

      {jobs.length === 0 ? (
        <div className="empty-state">
          <p>No crawl jobs yet. Start a new crawl to see it here.</p>
        </div>
      ) : (
        <div className="jobs-table">
          <table>
            <thead>
              <tr>
                <th>URL</th>
                <th>Status</th>
                <th>Pages Found</th>
                <th>Created</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {jobs.map((job) => (
                <tr key={job.id}>
                  <td className="url-cell">{job.inputUrl}</td>
                  <td>
                    {formatJobStatus(job)}
                  </td>
                  <td>{job.totalPagesFound || job.pagesProcessed || 0}</td>
                  <td>{new Date(job.createdAt).toLocaleString()}</td>
                  <td>
                    <button
                      onClick={() => onSelectJob(job.id)}
                      className="btn-view"
                    >
                      View
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="pagination">
        <button
          onClick={() => setPage(p => Math.max(1, p - 1))}
          disabled={page === 1}
          className="btn-secondary"
        >
          Previous
        </button>
        <span>Page {page} of {totalPages}</span>
        <button
          onClick={() => setPage(p => p + 1)}
          disabled={page >= totalPages}
          className="btn-secondary"
        >
          Next
        </button>
      </div>
    </div>
  )
}

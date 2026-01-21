import { useState, useEffect } from 'react'
import '../styles/screens.css'

export default function History({ onSelectJob }) {
  const [jobs, setJobs] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(0)

  useEffect(() => {
    const fetchJobs = async () => {
      setLoading(true)
      try {
        const response = await fetch(`/api/jobs/history?page=${page}&pageSize=10`)
        if (!response.ok) throw new Error('Failed to fetch history')
        const data = await response.json()
        setJobs(data.jobs)
        setTotalPages(data.totalPages)
      } catch (err) {
        setError(err.message)
      } finally {
        setLoading(false)
      }
    }

    fetchJobs()
  }, [page])

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
      <h2>Crawl History</h2>

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
                    <span className="status-badge" style={{ color: statusColor[job.status] }}>
                      {job.status}
                    </span>
                  </td>
                  <td>{job.totalPagesFound || 0}</td>
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

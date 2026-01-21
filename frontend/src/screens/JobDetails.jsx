import { useState, useEffect } from 'react'
import { API_BASE_URL } from '../config'
import '../styles/screens.css'

export default function JobDetails({ jobId, onBack }) {
  const [job, setJob] = useState(null)
  const [details, setDetails] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    const pollJob = async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/api/jobs/${jobId}`)
        if (!response.ok) throw new Error('Failed to fetch job')
        const jobData = await response.json()
        setJob(jobData)

        if (jobData.status === 'Completed' || jobData.status === 'Failed') {
          const detailsResponse = await fetch(`${API_BASE_URL}/api/jobs/${jobId}/details`)
          if (detailsResponse.ok) {
            const detailsData = await detailsResponse.json()
            setDetails(detailsData)
          }
          setLoading(false)
        }
      } catch (err) {
        setError(err.message)
        setLoading(false)
      }
    }

    pollJob()
    const interval = setInterval(pollJob, 2000)
    return () => clearInterval(interval)
  }, [jobId])

  if (loading && !job) {
    return (
      <div className="screen job-details">
        <div className="loading">Loading job details...</div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="screen job-details">
        <div className="error-message">{error}</div>
        <button onClick={onBack} className="btn-secondary">
          Back
        </button>
      </div>
    )
  }

  const statusColor = {
    'Pending': '#666',
    'Running': '#2563eb',
    'Completed': '#059669',
    'Failed': '#dc2626',
    'Canceled': '#666'
  }

  return (
    <div className="screen job-details">
      <button onClick={onBack} className="btn-back">← Back</button>

      <div className="job-header">
        <h2>Job Details</h2>
        <div className="job-info">
          <div className="info-item">
            <span className="label">Job ID:</span>
            <span className="value">{job?.id}</span>
          </div>
          <div className="info-item">
            <span className="label">URL:</span>
            <span className="value">{job?.inputUrl}</span>
          </div>
        </div>
      </div>

      <div className="job-status">
        <div className="status-row">
          <span className="label">Status:</span>
          <span className="status" style={{ color: statusColor[job?.status] }}>
            {job?.status}
          </span>
        </div>
        <div className="status-row">
          <span className="label">Pages Found:</span>
          <span className="value">{job?.totalPagesFound || 0}</span>
        </div>
        {job?.startedAt && (
          <div className="status-row">
            <span className="label">Started:</span>
            <span className="value">{new Date(job.startedAt).toLocaleString()}</span>
          </div>
        )}
        {job?.completedAt && (
          <div className="status-row">
            <span className="label">Completed:</span>
            <span className="value">{new Date(job.completedAt).toLocaleString()}</span>
          </div>
        )}
        {job?.failureReason && (
          <div className="status-row error">
            <span className="label">Error:</span>
            <span className="value">{job.failureReason}</span>
          </div>
        )}
      </div>

      {job?.status === 'Running' && (
        <div className="progress-container">
          <div className="spinner"></div>
          <p>Crawling in progress...</p>
        </div>
      )}

      {details && job?.status === 'Completed' && (
        <div className="pages-tree">
          <h3>Discovered Pages</h3>
          <PageTree pages={details.pages} />
        </div>
      )}
    </div>
  )
}

function PageTree({ pages }) {
  return (
    <ul className="tree">
      {pages.map((page, idx) => (
        <li key={idx}>
          <div className="tree-node">
            <a href={page.url} target="_blank" rel="noopener noreferrer">
              {page.title || page.url}
            </a>
            {page.domainLinkRatio !== null && (
              <span className="ratio">
                {(page.domainLinkRatio * 100).toFixed(1)}% internal
              </span>
            )}
          </div>
          {page.children?.length > 0 && (
            <PageTree pages={page.children} />
          )}
        </li>
      ))}
    </ul>
  )
}

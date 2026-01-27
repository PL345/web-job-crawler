import { useReducer, useCallback, memo } from 'react'
import { useJobPolling } from '../hooks/useJobPolling'
import { apiClient, ApiError } from '../utils/apiClient'
import { Button, Card, StatusBadge, Alert, ProgressIndicator } from '../components/ui'
import '../styles/screens.css'

function jobDetailsReducer(state, action) {
  switch (action.type) {
    case 'SET_JOB':
      return {
        ...state,
        job: action.payload,
        error: ''
      }
    case 'SET_DETAILS':
      return {
        ...state,
        details: action.payload
      }
    case 'SET_ERROR':
      return {
        ...state,
        error: action.payload
      }
    case 'SET_CANCELLING':
      return {
        ...state,
        cancelling: action.payload
      }
    case 'CLEAR_ERROR':
      return {
        ...state,
        error: ''
      }
    default:
      return state
  }
}

const initialState = {
  job: null,
  details: null,
  error: '',
  cancelling: false
}

export default function JobDetails({ jobId, onBack }) {
  const [state, dispatch] = useReducer(jobDetailsReducer, initialState)
  const { job, details, error, cancelling } = state

  useJobPolling(jobId, useCallback(async (jobData) => {
    dispatch({ type: 'SET_JOB', payload: jobData })
    
    if ((jobData.status === 'Completed' || jobData.status === 'Failed') && !details) {
      try {
        const detailsData = await apiClient.getJobDetails(jobId)
        dispatch({ type: 'SET_DETAILS', payload: detailsData })
      } catch (err) {
        console.error('Failed to fetch job details:', err)
      }
    }
  }, [details]), { stopOnTerminal: true })

  const handleCancelJob = useCallback(async () => {
    if (!confirm('Are you sure you want to cancel this job?')) return
    dispatch({ type: 'SET_CANCELLING', payload: true })
    try {
      await apiClient.cancelJob(jobId)
    } catch (err) {
      dispatch({ 
        type: 'SET_ERROR', 
        payload: err instanceof ApiError ? err.message : 'Failed to cancel job'
      })
    } finally {
      dispatch({ type: 'SET_CANCELLING', payload: false })
    }
  }, [jobId])

  if (!job) {
    return (
      <div className="screen job-details">
        <Alert type="info" message="Loading job details..." dismissible={false} />
      </div>
    )
  }

  const isTerminal = ['Completed', 'Failed', 'Cancelled'].includes(job.status)
  const progress = job.totalPagesFound || job.pagesProcessed || 0

  return (
    <div className="screen job-details">
      {error && <Alert type="error" message={error} onClose={() => dispatch({ type: 'CLEAR_ERROR' })} />}
      
      <Button variant="secondary" onClick={onBack} className="mb-20">← Back</Button>

      <Card className="job-header">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h2>Job Details</h2>
          {(job.status === 'Pending' || job.status === 'Running') && (
            <Button
              variant="danger"
              size="sm"
              onClick={handleCancelJob}
              loading={cancelling}
            >
              {cancelling ? 'Cancelling...' : 'Cancel Job'}
            </Button>
          )}
        </div>
        <div className="job-info">
          <div className="info-item">
            <span className="label">Job ID:</span>
            <span className="value">{job.id}</span>
          </div>
          <div className="info-item">
            <span className="label">URL:</span>
            <span className="value">{job.inputUrl}</span>
          </div>
        </div>
      </Card>

      <Card className="job-status">
        <div className="status-row">
          <span className="label">Status:</span>
          <StatusBadge status={job.status} />
        </div>
        <div className="status-row">
          <span className="label">Pages Found:</span>
          <span className="value">{progress}</span>
        </div>
        {job.status === 'Running' && job.pagesProcessed > 0 && (
          <div className="status-row">
            <span className="label">Pages Processed:</span>
            <span className="value">{job.pagesProcessed}</span>
          </div>
        )}
        {job.currentUrl && (
          <div className="status-row">
            <span className="label">Currently Crawling:</span>
            <span className="value current-url" title={job.currentUrl}>
              {job.currentUrl}
            </span>
          </div>
        )}
        {job.startedAt && (
          <div className="status-row">
            <span className="label">Started:</span>
            <span className="value">{new Date(job.startedAt).toLocaleString()}</span>
          </div>
        )}
        {job.completedAt && (
          <div className="status-row">
            <span className="label">Completed:</span>
            <span className="value">{new Date(job.completedAt).toLocaleString()}</span>
          </div>
        )}
      </Card>

      {job.status === 'Running' && (
        <Card className="progress-section">
          <div className="spinner" />
          <div className="progress-text">
            <p>Crawling in progress...</p>
            {job.pagesProcessed > 0 && (
              <>
                <ProgressIndicator current={job.pagesProcessed} total={job.pagesProcessed + 1} label="Progress" />
                <p className="progress-stats">{job.pagesProcessed} pages processed</p>
              </>
            )}
            {job.currentUrl && (
              <p className="current-page">
                <strong>Currently processing:</strong><br />
                <span className="url-text">{job.currentUrl}</span>
              </p>
            )}
          </div>
        </Card>
      )}

      {job.status === 'Failed' && (
        <Alert type="error" message={job.failureReason || 'An unknown error occurred'} dismissible={false} />
      )}

      {details && isTerminal && (
        <Card>
          <h3 className="mb-16">Discovered Pages</h3>
          {details.pages.length > 0 ? (
            <PageTree pages={details.pages} />
          ) : (
            <p className="text-muted">No pages discovered.</p>
          )}
        </Card>
      )}
    </div>
  )
}

const PageTree = memo(function PageTree({ pages }) {
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
})

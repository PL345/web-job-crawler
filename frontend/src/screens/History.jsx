import { useReducer, useEffect, useRef, useCallback, memo } from 'react'
import { apiClient, ApiError } from '../utils/apiClient'
import { Button, Card, StatusBadge, Skeleton, Alert } from '../components/ui'
import '../styles/screens.css'

function historyReducer(state, action) {
  switch (action.type) {
    case 'SET_JOBS':
      return {
        ...state,
        jobs: action.payload.jobs,
        totalPages: action.payload.totalPages
      }
    case 'SET_LOADING':
      return { ...state, loading: action.payload }
    case 'SET_UPDATING':
      return { ...state, isUpdating: action.payload }
    case 'SET_ERROR':
      return { ...state, error: action.payload }
    case 'CLEAR_ERROR':
      return { ...state, error: '' }
    case 'SET_PAGE':
      return { ...state, page: action.payload }
    case 'SET_CANCELLING':
      return { ...state, cancellingJobId: action.payload }
    default:
      return state
  }
}

const initialState = {
  jobs: [],
  loading: true,
  error: '',
  page: 1,
  totalPages: 0,
  isUpdating: false,
  cancellingJobId: null
}

export default function History({ onSelectJob }) {
  const [state, dispatch] = useReducer(historyReducer, initialState)
  const { jobs, loading, error, page, totalPages, isUpdating, cancellingJobId } = state

  const pollTimer = useRef(null)
  const abortRef = useRef(null)
  const isMounted = useRef(true)

  const isTabVisible = () => typeof document !== 'undefined' ? document.visibilityState === 'visible' : true

  const clearTimer = () => {
    if (pollTimer.current) {
      clearTimeout(pollTimer.current)
      pollTimer.current = null
    }
  }

  const fetchJobs = useCallback(async (isInitialLoad = false) => {
    dispatch({ type: isInitialLoad ? 'SET_LOADING' : 'SET_UPDATING', payload: true })

    if (abortRef.current) abortRef.current.abort()
    abortRef.current = new AbortController()

    try {
      const data = await apiClient.getJobHistory(page, 10, { signal: abortRef.current.signal })
      if (isMounted.current) {
        dispatch({ type: 'SET_JOBS', payload: { jobs: data.jobs, totalPages: data.totalPages } })
        dispatch({ type: 'CLEAR_ERROR' })
      }
    } catch (err) {
      if (err.name !== 'AbortError' && isMounted.current) {
        dispatch({ type: 'SET_ERROR', payload: err instanceof ApiError ? err.message : 'Failed to load history' })
      }
    } finally {
      dispatch({ type: isInitialLoad ? 'SET_LOADING' : 'SET_UPDATING', payload: false })
    }
  }, [page])

  const handleCancelJob = useCallback(async (jobId) => {
    if (!confirm('Are you sure you want to cancel this job?')) return

    dispatch({ type: 'SET_CANCELLING', payload: jobId })
    try {
      await apiClient.cancelJob(jobId)
      await fetchJobs(false)
    } catch (err) {
      dispatch({ type: 'SET_ERROR', payload: err instanceof ApiError ? err.message : 'Failed to cancel job' })
    } finally {
      dispatch({ type: 'SET_CANCELLING', payload: null })
    }
  }, [fetchJobs])

  useEffect(() => {
    isMounted.current = true

    const schedulePoll = (delayMs) => {
      clearTimer()
      pollTimer.current = setTimeout(() => {
        if (isMounted.current) {
          fetchJobs(false)
          const nextDelay = isTabVisible() ? 3000 : 6000
          schedulePoll(nextDelay)
        }
      }, delayMs)
    }

    fetchJobs(true)
    const nextDelay = isTabVisible() ? 3000 : 6000
    schedulePoll(nextDelay)

    const handleVisibility = () => {
      const delay = isTabVisible() ? 3000 : 6000
      clearTimer()
      schedulePoll(delay)
    }

    document.addEventListener('visibilitychange', handleVisibility)

    return () => {
      isMounted.current = false
      clearTimer()
      if (abortRef.current) abortRef.current.abort()
      document.removeEventListener('visibilitychange', handleVisibility)
    }
  }, [fetchJobs])

  if (loading) {
    return (
      <div className="screen history">
        <Skeleton height="160px" />
        <Skeleton height="160px" />
      </div>
    )
  }

  return (
    <div className="screen history">
      <div className="history-header">
        <h2>Crawl History</h2>
        {isUpdating && <div className="update-indicator">●</div>}
      </div>

      {error && <Alert type="error" message={error} onClose={() => dispatch({ type: 'CLEAR_ERROR' })} />}

      {jobs.length === 0 ? (
        <Card className="empty-state">
          <p>No crawl jobs yet. Start a new crawl to see it here.</p>
        </Card>
      ) : (
        <Card>
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
                      <JobStatusDisplay job={job} />
                    </td>
                    <td>{job.totalPagesFound || job.pagesProcessed || 0}</td>
                    <td>{new Date(job.createdAt).toLocaleString()}</td>
                    <td>
                      <div style={{ display: 'flex', gap: '8px' }}>
                        <Button
                          variant="primary"
                          size="sm"
                          onClick={() => onSelectJob(job.id)}
                        >
                          View
                        </Button>
                        {(job.status === 'Pending' || job.status === 'Running') && (
                          <Button
                            variant="danger"
                            size="sm"
                            onClick={() => handleCancelJob(job.id)}
                            loading={cancellingJobId === job.id}
                          >
                            {cancellingJobId === job.id ? 'Cancelling...' : 'Cancel'}
                          </Button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      <div className="pagination">
        <Button
          variant="secondary"
          size="sm"
          onClick={() => dispatch({ type: 'SET_PAGE', payload: Math.max(1, page - 1) })}
          disabled={page === 1}
        >
          Previous
        </Button>
        <span>Page {page} of {totalPages}</span>
        <Button
          variant="secondary"
          size="sm"
          onClick={() => dispatch({ type: 'SET_PAGE', payload: page + 1 })}
          disabled={page >= totalPages}
        >
          Next
        </Button>
      </div>
    </div>
  )
}

const JobStatusDisplay = memo(function JobStatusDisplay({ job }) {
  if (job.status === 'Running') {
    return (
      <div className="running-status">
        <StatusBadge status={job.status} />
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

  if (job.status === 'Failed') {
    return (
      <div className="failed-status">
        <StatusBadge status={job.status} />
        {job.failureReason && (
          <small className="failure-hint" title={job.failureReason}>
            {job.failureReason.length > 50 ? job.failureReason.substring(0, 50) + '...' : job.failureReason}
          </small>
        )}
      </div>
    )
  }

  return <StatusBadge status={job.status} />
})

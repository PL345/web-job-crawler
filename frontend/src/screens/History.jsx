import { useState, useEffect, useRef, useCallback, memo } from 'react'
import { apiClient, ApiError } from '../utils/apiClient'
import { Button, Card, StatusBadge, Skeleton, Alert } from '../components/ui'
import '../styles/screens.css'

export default function History({ onSelectJob }) {
  const [jobs, setJobs] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(0)
  const [isUpdating, setIsUpdating] = useState(false)
  const [cancellingJobId, setCancellingJobId] = useState(null)

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
    if (isInitialLoad) setLoading(true)
    else setIsUpdating(true)

    if (abortRef.current) abortRef.current.abort()
    abortRef.current = new AbortController()

    try {
      const data = await apiClient.getJobHistory(page, 10, { signal: abortRef.current.signal })
      if (isMounted.current) {
        setJobs(data.jobs)
        setTotalPages(data.totalPages)
        setError('')
      }
    } catch (err) {
      if (err.name !== 'AbortError' && isMounted.current) {
        setError(err instanceof ApiError ? err.message : 'Failed to load history')
      }
    } finally {
      if (isInitialLoad) setLoading(false)
      else setIsUpdating(false)
    }
  }, [page])

  const handleCancelJob = useCallback(async (jobId) => {
    if (!confirm('Are you sure you want to cancel this job?')) return

    setCancellingJobId(jobId)
    try {
      await apiClient.cancelJob(jobId)
      await fetchJobs(false)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to cancel job')
    } finally {
      setCancellingJobId(null)
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

      {error && <Alert type="error" message={error} onClose={() => setError('')} />}

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
          onClick={() => setPage(p => Math.max(1, p - 1))}
          disabled={page === 1}
        >
          Previous
        </Button>
        <span>Page {page} of {totalPages}</span>
        <Button
          variant="secondary"
          size="sm"
          onClick={() => setPage(p => p + 1)}
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

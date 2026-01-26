import { useEffect, useRef, useCallback } from 'react'
import { apiClient } from '../utils/apiClient'

/**
 * Custom hook for polling job status with visibility awareness and cleanup
 * @param {string} jobId - Job ID to poll
 * @param {function} onJobUpdate - Callback when job data is fetched
 * @param {object} options - { stopOnTerminal, initialFetch, pollIntervalMs, hiddenIntervalMs }
 */
export const useJobPolling = (jobId, onJobUpdate, options = {}) => {
  const {
    stopOnTerminal = true,
    initialFetch = true,
    pollIntervalMs = 2000,
    hiddenIntervalMs = 5000
  } = options

  const pollTimer = useRef(null)
  const abortRef = useRef(null)
  const isMounted = useRef(true)

  const isTabVisible = useCallback(() => {
    return typeof document !== 'undefined' ? document.visibilityState === 'visible' : true
  }, [])

  const clearTimer = useCallback(() => {
    if (pollTimer.current) {
      clearTimeout(pollTimer.current)
      pollTimer.current = null
    }
  }, [])

  const scheduleNextPoll = useCallback((delayMs) => {
    clearTimer()
    pollTimer.current = setTimeout(() => poll(false), delayMs)
  }, [clearTimer])

  const poll = useCallback(async (isInitial = false) => {
    if (!isMounted.current) return

    if (abortRef.current) abortRef.current.abort()
    abortRef.current = new AbortController()

    try {
      const jobData = await apiClient.getJob(jobId, { signal: abortRef.current.signal })
      if (isMounted.current) {
        onJobUpdate(jobData)
        
        const isTerminal = ['Completed', 'Failed', 'Cancelled'].includes(jobData.status)
        
        if (!(stopOnTerminal && isTerminal)) {
          const delay = isTabVisible() ? pollIntervalMs : hiddenIntervalMs
          scheduleNextPoll(delay)
        }
      }
    } catch (error) {
      if (error.name !== 'AbortError' && isMounted.current) {
        console.error('Polling error:', error)
        // Optionally reschedule on error with backoff
        scheduleNextPoll(pollIntervalMs * 2)
      }
    }
  }, [jobId, onJobUpdate, stopOnTerminal, pollIntervalMs, hiddenIntervalMs, isTabVisible, scheduleNextPoll])

  useEffect(() => {
    isMounted.current = true

    if (initialFetch) {
      poll(true)
    }

    const handleVisibilityChange = () => {
      if (!pollTimer.current && document.visibilityState === 'visible') {
        poll(false)
      }
    }

    document.addEventListener('visibilitychange', handleVisibilityChange)

    return () => {
      isMounted.current = false
      document.removeEventListener('visibilitychange', handleVisibilityChange)
      if (abortRef.current) abortRef.current.abort()
      clearTimer()
    }
  }, [jobId, initialFetch, poll, clearTimer])

  return { poll, clearTimer }
}

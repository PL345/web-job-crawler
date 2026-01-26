import { API_BASE_URL } from '../config'

class ApiError extends Error {
  constructor(status, message, details = null) {
    super(message)
    this.status = status
    this.details = details
    this.name = 'ApiError'
  }
}

class ApiClient {
  async request(endpoint, options = {}) {
    const url = `${API_BASE_URL}${endpoint}`
    const config = {
      headers: {
        'Content-Type': 'application/json',
        ...options.headers
      },
      ...options
    }

    try {
      const response = await fetch(url, config)

      if (!response.ok) {
        let details = null
        try {
          details = await response.json()
        } catch {
          details = { error: response.statusText }
        }
        throw new ApiError(response.status, details.error || 'Request failed', details)
      }

      return await response.json()
    } catch (error) {
      if (error instanceof ApiError) throw error
      if (error.name === 'AbortError') throw error
      throw new ApiError(0, error.message || 'Network error')
    }
  }

  // Job endpoints
  getJob(jobId, options = {}) {
    return this.request(`/jobs/${jobId}`, { ...options, method: 'GET' })
  }

  getJobDetails(jobId, options = {}) {
    return this.request(`/jobs/${jobId}/details`, { ...options, method: 'GET' })
  }

  getJobHistory(page = 1, pageSize = 10, options = {}) {
    return this.request(`/jobs/history?page=${page}&pageSize=${pageSize}`, { ...options, method: 'GET' })
  }

  createJob(url, maxDepth = 2, options = {}) {
    return this.request('/jobs/create', {
      ...options,
      method: 'POST',
      body: JSON.stringify({ url, maxDepth: parseInt(maxDepth) })
    })
  }

  cancelJob(jobId, options = {}) {
    return this.request(`/jobs/${jobId}/cancel`, { ...options, method: 'POST' })
  }

  getHealth(options = {}) {
    return this.request('/jobs/health', { ...options, method: 'GET' })
  }
}

export const apiClient = new ApiClient()
export { ApiError }

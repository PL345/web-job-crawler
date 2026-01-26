import { useState } from 'react'
import { apiClient, ApiError } from '../utils/apiClient'
import { Button, Card, Alert } from '../components/ui'
import '../styles/screens.css'

export default function StartCrawl({ onJobCreated }) {
  const [url, setUrl] = useState('')
  const [maxDepth, setMaxDepth] = useState(2)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError('')
    setLoading(true)

    try {
      const data = await apiClient.createJob(url, parseInt(maxDepth))
      onJobCreated(data.jobId)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to create job')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="screen start-crawl">
      <Card className="form-container">
        <h2>Start a New Crawl</h2>
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="url">Website URL</label>
            <input
              id="url"
              type="url"
              value={url}
              onChange={(e) => setUrl(e.target.value)}
              placeholder="https://example.com"
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="depth">Max Depth</label>
            <select
              id="depth"
              value={maxDepth}
              onChange={(e) => setMaxDepth(e.target.value)}
            >
              <option value="1">1</option>
              <option value="2">2</option>
              <option value="3">3</option>
              <option value="4">4</option>
              <option value="5">5</option>
            </select>
          </div>

          {error && <Alert type="error" message={error} onClose={() => setError('')} />}

          <Button type="submit" loading={loading} disabled={loading}>
            {loading ? 'Starting...' : 'Start Crawl'}
          </Button>
        </form>
      </Card>
    </div>
  )
}

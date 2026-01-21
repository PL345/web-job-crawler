import { useState } from 'react'
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
      const response = await fetch('/api/jobs/create', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ url, maxDepth: parseInt(maxDepth) })
      })

      if (!response.ok) {
        const data = await response.json()
        throw new Error(data.error || 'Failed to create job')
      }

      const data = await response.json()
      onJobCreated(data.jobId)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="screen start-crawl">
      <div className="form-container">
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

          {error && <div className="error-message">{error}</div>}

          <button type="submit" disabled={loading} className="btn-primary">
            {loading ? 'Starting...' : 'Start Crawl'}
          </button>
        </form>
      </div>
    </div>
  )
}

import { useState, lazy, Suspense } from 'react'
import './styles/App.css'

const StartCrawl = lazy(() => import('./screens/StartCrawl'))
const JobDetails = lazy(() => import('./screens/JobDetails'))
const History = lazy(() => import('./screens/History'))

export default function App() {
  const [currentScreen, setCurrentScreen] = useState('start')
  const [selectedJobId, setSelectedJobId] = useState(null)

  const handleJobCreated = (jobId) => {
    setSelectedJobId(jobId)
    setCurrentScreen('details')
  }

  const handleViewHistory = () => {
    setCurrentScreen('history')
  }

  const handleSelectJob = (jobId) => {
    setSelectedJobId(jobId)
    setCurrentScreen('details')
  }

  const handleBack = () => {
    setCurrentScreen('start')
  }

  return (
    <div className="app">
      <header className="app-header">
        <h1>Web Crawler</h1>
        <nav className="app-nav">
          <button
            onClick={handleBack}
            className={currentScreen === 'start' ? 'nav-btn active' : 'nav-btn'}
          >
            New Crawl
          </button>
          <button
            onClick={handleViewHistory}
            className={currentScreen === 'history' ? 'nav-btn active' : 'nav-btn'}
          >
            History
          </button>
        </nav>
      </header>

      <main className="app-main">
        <Suspense fallback={<div className="loading-fallback">Loading...</div>}>
          {currentScreen === 'start' && (
            <StartCrawl onJobCreated={handleJobCreated} />
          )}
          {currentScreen === 'details' && selectedJobId && (
            <JobDetails jobId={selectedJobId} onBack={handleBack} />
          )}
          {currentScreen === 'history' && (
            <History onSelectJob={handleSelectJob} />
          )}
        </Suspense>
      </main>
    </div>
  )
}

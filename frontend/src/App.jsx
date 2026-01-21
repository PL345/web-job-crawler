import { useState } from 'react'
import StartCrawl from './screens/StartCrawl'
import JobDetails from './screens/JobDetails'
import History from './screens/History'
import './App.css'

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
        {currentScreen === 'start' && (
          <StartCrawl onJobCreated={handleJobCreated} />
        )}
        {currentScreen === 'details' && selectedJobId && (
          <JobDetails jobId={selectedJobId} onBack={handleBack} />
        )}
        {currentScreen === 'history' && (
          <History onSelectJob={handleSelectJob} />
        )}
      </main>
    </div>
  )
}

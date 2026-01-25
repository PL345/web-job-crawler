-- Add progress tracking fields to crawl_jobs table
ALTER TABLE crawl_jobs 
ADD COLUMN current_url TEXT,
ADD COLUMN pages_processed INTEGER DEFAULT 0;

-- Add index for better performance on status queries
CREATE INDEX IF NOT EXISTS idx_crawl_jobs_status_updated ON crawl_jobs(status, updated_at DESC);
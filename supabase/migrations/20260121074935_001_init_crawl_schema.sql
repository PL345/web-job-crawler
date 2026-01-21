/*
  # Web Crawler Job System Schema
  
  ## Overview
  This migration establishes the core schema for a distributed web crawling system
  with job orchestration, page tracking, and link relationships.
  
  ## Tables
  1. crawl_jobs - Main job records with status lifecycle
  2. crawled_pages - Individual pages discovered during a crawl
  3. page_links - Relationships between pages (parent → child links)
  4. job_events - Event log for audit trail and debugging
  
  ## Security
  - RLS enabled on all tables
  - Public read access for API service account
  - Job creator isolation for future user-based filtering
*/

-- Create jobs table
CREATE TABLE IF NOT EXISTS crawl_jobs (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  input_url text NOT NULL,
  max_depth int NOT NULL DEFAULT 2,
  status text NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending', 'Running', 'Completed', 'Failed', 'Canceled')),
  created_at timestamptz NOT NULL DEFAULT now(),
  started_at timestamptz,
  completed_at timestamptz,
  failure_reason text,
  total_pages_found int DEFAULT 0,
  created_by uuid,
  updated_at timestamptz NOT NULL DEFAULT now()
);

-- Create crawled_pages table
CREATE TABLE IF NOT EXISTS crawled_pages (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  job_id uuid NOT NULL REFERENCES crawl_jobs(id) ON DELETE CASCADE,
  url text NOT NULL,
  normalized_url text NOT NULL,
  title text,
  status_code int,
  domain_link_ratio numeric(5, 4),
  outgoing_links_count int DEFAULT 0,
  internal_links_count int DEFAULT 0,
  crawled_at timestamptz DEFAULT now(),
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE(job_id, normalized_url)
);

-- Create page_links table (parent → child relationships)
CREATE TABLE IF NOT EXISTS page_links (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  job_id uuid NOT NULL REFERENCES crawl_jobs(id) ON DELETE CASCADE,
  source_page_id uuid NOT NULL REFERENCES crawled_pages(id) ON DELETE CASCADE,
  target_url text NOT NULL,
  link_text text,
  created_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE(source_page_id, target_url)
);

-- Create job_events table for audit trail
CREATE TABLE IF NOT EXISTS job_events (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  job_id uuid NOT NULL REFERENCES crawl_jobs(id) ON DELETE CASCADE,
  event_type text NOT NULL,
  event_data jsonb,
  correlation_id uuid,
  created_at timestamptz NOT NULL DEFAULT now()
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_crawl_jobs_status ON crawl_jobs(status);
CREATE INDEX IF NOT EXISTS idx_crawl_jobs_created_at ON crawl_jobs(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_crawled_pages_job_id ON crawled_pages(job_id);
CREATE INDEX IF NOT EXISTS idx_crawled_pages_normalized_url ON crawled_pages(job_id, normalized_url);
CREATE INDEX IF NOT EXISTS idx_page_links_job_id ON page_links(job_id);
CREATE INDEX IF NOT EXISTS idx_page_links_source ON page_links(source_page_id);
CREATE INDEX IF NOT EXISTS idx_job_events_job_id ON job_events(job_id);
CREATE INDEX IF NOT EXISTS idx_job_events_correlation_id ON job_events(correlation_id);

-- Enable RLS (restrictive by default)
ALTER TABLE crawl_jobs ENABLE ROW LEVEL SECURITY;
ALTER TABLE crawled_pages ENABLE ROW LEVEL SECURITY;
ALTER TABLE page_links ENABLE ROW LEVEL SECURITY;
ALTER TABLE job_events ENABLE ROW LEVEL SECURITY;

-- Public read policies (for API service account)
CREATE POLICY "jobs_public_read" ON crawl_jobs
  FOR SELECT TO public
  USING (true);

CREATE POLICY "pages_public_read" ON crawled_pages
  FOR SELECT TO public
  USING (true);

CREATE POLICY "links_public_read" ON page_links
  FOR SELECT TO public
  USING (true);

CREATE POLICY "events_public_read" ON job_events
  FOR SELECT TO public
  USING (true);

-- Public insert/update policies (for API and worker services)
CREATE POLICY "jobs_public_insert" ON crawl_jobs
  FOR INSERT TO public
  WITH CHECK (true);

CREATE POLICY "jobs_public_update" ON crawl_jobs
  FOR UPDATE TO public
  USING (true)
  WITH CHECK (true);

CREATE POLICY "pages_public_insert" ON crawled_pages
  FOR INSERT TO public
  WITH CHECK (true);

CREATE POLICY "pages_public_update" ON crawled_pages
  FOR UPDATE TO public
  USING (true)
  WITH CHECK (true);

CREATE POLICY "links_public_insert" ON page_links
  FOR INSERT TO public
  WITH CHECK (true);

CREATE POLICY "events_public_insert" ON job_events
  FOR INSERT TO public
  WITH CHECK (true);
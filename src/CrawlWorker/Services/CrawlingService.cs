using HtmlAgilityPack;
using SharedDomain.Models;
using SharedDomain.Utilities;
using CrawlWorker.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CrawlWorker.Services;

public interface ICrawlingService
{
    Task CrawlAsync(Guid jobId, string startUrl, int maxDepth);
}

public class CrawlingService : ICrawlingService
{
    private readonly CrawlerDbContext _db;
    private readonly ILogger<CrawlingService> _logger;
    private readonly HttpClient _httpClient;
    private const int MaxPagesPerJob = 200;
    private const int TimeoutSeconds = 10;

    public CrawlingService(CrawlerDbContext db, ILogger<CrawlingService> logger, HttpClient httpClient)
    {
        _db = db;
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
        
        // Add consistent headers to get more predictable responses
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", 
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
    }

    public async Task CrawlAsync(Guid jobId, string startUrl, int maxDepth)
    {
        var job = await _db.CrawlJobs.FindAsync(jobId);
        if (job == null)
        {
            _logger.LogError("Job {JobId} not found", jobId);
            return;
        }

        try
        {
            job.Status = "Running";
            job.StartedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var normalizedStartUrl = UrlNormalizer.Normalize(startUrl);
            var startDomain = UrlNormalizer.GetDomain(startUrl);

            var urlsToProcess = new Queue<(string Url, int Depth)>();
            var processedUrls = new HashSet<string>();

            urlsToProcess.Enqueue((normalizedStartUrl, 0));

            while (urlsToProcess.Count > 0 && processedUrls.Count < MaxPagesPerJob)
            {
                var (url, depth) = urlsToProcess.Dequeue();

                if (string.IsNullOrEmpty(url) || processedUrls.Contains(url) || depth > maxDepth)
                    continue;

                processedUrls.Add(url);

                // Update progress before crawling each page
                job.CurrentUrl = url;
                job.PagesProcessed = processedUrls.Count;
                job.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                await CrawlPageAsync(jobId, url, startUrl, depth, urlsToProcess, startDomain, maxDepth, processedUrls);

                await Task.Delay(500);
            }

            job.Status = "Completed";
            job.CompletedAt = DateTime.UtcNow;
            job.TotalPagesFound = processedUrls.Count;
            job.PagesProcessed = processedUrls.Count;
            job.CurrentUrl = null; // Clear current URL when completed
            job.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Completed crawling job {JobId} - found {PageCount} pages", jobId, processedUrls.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crawling job {JobId}", jobId);
            job.Status = "Failed";
            job.FailureReason = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    private async Task CrawlPageAsync(Guid jobId, string pageUrl, string startUrl, int depth,
        Queue<(string, int)> urlsToProcess, string startDomain, int maxDepth,
        HashSet<string> processedUrls)
    {
        try
        {
            var existingPage = await _db.CrawledPages
                .FirstOrDefaultAsync(p => p.JobId == jobId && p.NormalizedUrl == pageUrl);

            if (existingPage != null)
                return;

            var response = await GetPageWithRetryAsync(pageUrl);
            if (response == null) return;
            
            var statusCode = (int)response.StatusCode;

            if (!response.Content.Headers.ContentType?.MediaType?.StartsWith("text/html") ?? false)
            {
                _logger.LogInformation("Skipping non-HTML page {Url}", pageUrl);
                return;
            }

            var content = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var titleNode = doc.DocumentNode.SelectSingleNode("//title");
            var title = titleNode?.InnerText ?? string.Empty;

            var links = doc.DocumentNode.SelectNodes("//a[@href]") ?? new HtmlNodeCollection(null);
            var discoveredUrls = new List<string>();
            var internalLinkCount = 0;
            var externalLinkCount = 0;

            foreach (var link in links)
            {
                var href = link.GetAttributeValue("href", string.Empty);
                var resolvedUrl = UrlNormalizer.ResolveRelativeUrl(pageUrl, href);

                if (string.IsNullOrEmpty(resolvedUrl))
                    continue;

                discoveredUrls.Add(resolvedUrl);

                // Save page link relationship
                var pageLink = new PageLink
                {
                    Id = Guid.NewGuid(),
                    JobId = jobId,
                    SourcePageId = Guid.Empty, // Will be set after page is saved
                    TargetUrl = resolvedUrl,
                    LinkText = link.InnerText?.Trim() ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                if (UrlNormalizer.IsSameDomain(resolvedUrl, startUrl))
                    internalLinkCount++;
                else
                    externalLinkCount++;

                if (depth < maxDepth && !processedUrls.Contains(resolvedUrl))
                    urlsToProcess.Enqueue((resolvedUrl, depth + 1));
            }

            var domainLinkRatio = (discoveredUrls.Count > 0)
                ? (decimal)internalLinkCount / discoveredUrls.Count
                : 0m;

            var crawledPage = new CrawledPage
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                Url = pageUrl,
                NormalizedUrl = pageUrl,
                Title = title,
                StatusCode = statusCode,
                DomainLinkRatio = domainLinkRatio,
                OutgoingLinksCount = discoveredUrls.Count,
                InternalLinksCount = internalLinkCount,
                CrawledAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                _db.CrawledPages.Add(crawledPage);
                await _db.SaveChangesAsync();

                // Batch approach: get all existing links for this page first
                var existingTargetUrlsList = await _db.PageLinks
                    .Where(pl => pl.SourcePageId == crawledPage.Id)
                    .Select(pl => pl.TargetUrl)
                    .ToListAsync();
                
                var existingTargetUrls = new HashSet<string>(existingTargetUrlsList);

                // Remove duplicates from discovered URLs and filter out existing ones
                var uniqueNewUrls = discoveredUrls
                    .Distinct()
                    .Where(url => !existingTargetUrls.Contains(url))
                    .ToList();

                // Add only the new unique links
                foreach (var discoveredUrl in uniqueNewUrls)
                {
                    var pageLink = new PageLink
                    {
                        Id = Guid.NewGuid(),
                        JobId = jobId,
                        SourcePageId = crawledPage.Id,
                        TargetUrl = discoveredUrl,
                        LinkText = string.Empty,
                        CreatedAt = DateTime.UtcNow
                    };

                    _db.PageLinks.Add(pageLink);
                }
                
                if (uniqueNewUrls.Any())
                {
                    try
                    {
                        await _db.SaveChangesAsync();
                        _logger.LogInformation("Saved {NewLinkCount} new page links for {Url}", uniqueNewUrls.Count, pageUrl);
                    }
                    catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") ?? false)
                    {
                        _logger.LogWarning("Duplicate key error despite checking - this shouldn't happen for {Url}", pageUrl);
                        // Clear the context and continue
                        _db.ChangeTracker.Clear();
                    }
                }
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") ?? false)
            {
                _logger.LogInformation("Page {Url} already processed (idempotent)", pageUrl);
            }

            _logger.LogInformation("Crawled page {Url} - found {LinkCount} total links ({InternalLinks} internal, {ExternalLinks} external). Page size: {ContentLength} chars",
                pageUrl, discoveredUrls.Count, internalLinkCount, externalLinkCount, content.Length);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to crawl page {Url}", pageUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing page {Url}", pageUrl);
        }
    }

    private async Task<HttpResponseMessage?> GetPageWithRetryAsync(string url, int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return response;
                }
                
                _logger.LogWarning("Attempt {Attempt}/{MaxRetries} failed for {Url} with status {StatusCode}", 
                    attempt, maxRetries, url, response.StatusCode);
                
                if (attempt < maxRetries)
                {
                    await Task.Delay(1000 * attempt); // Exponential backoff
                }
                else
                {
                    return response; // Return the last response even if not successful
                }
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Attempt {Attempt}/{MaxRetries} failed for {Url}, retrying...", 
                    attempt, maxRetries, url);
                await Task.Delay(1000 * attempt); // Exponential backoff
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch {Url} on attempt {Attempt}", url, attempt);
                if (attempt == maxRetries) return null;
                await Task.Delay(1000 * attempt);
            }
        }
        
        return null;
        }
    }
}

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

                await CrawlPageAsync(jobId, url, startUrl, depth, urlsToProcess, startDomain, maxDepth, processedUrls);

                await Task.Delay(500);
            }

            job.Status = "Completed";
            job.CompletedAt = DateTime.UtcNow;
            job.TotalPagesFound = processedUrls.Count;
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

            var response = await _httpClient.GetAsync(pageUrl);
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
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") ?? false)
            {
                _logger.LogInformation("Page {Url} already processed (idempotent)", pageUrl);
            }

            _logger.LogInformation("Crawled page {Url} - found {LinkCount} links ({InternalLinks} internal)",
                pageUrl, discoveredUrls.Count, internalLinkCount);
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
}

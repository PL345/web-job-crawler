using CrawlAPI.Infrastructure;
using SharedDomain.Models;
using SharedDomain.Messages;
using Microsoft.EntityFrameworkCore;

namespace CrawlAPI.Services;

public interface IJobService
{
    Task<CrawlJob> CreateJobAsync(string url, int maxDepth);
    Task<CrawlJob?> GetJobAsync(Guid jobId);
    Task<(List<CrawlJob> Jobs, int Total)> GetJobHistoryAsync(int page, int pageSize);
    Task<JobDetailsDto> GetJobDetailsAsync(Guid jobId);
}

public class JobService : IJobService
{
    private readonly CrawlerDbContext _db;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<JobService> _logger;

    public JobService(CrawlerDbContext db, IMessagePublisher publisher, ILogger<JobService> logger)
    {
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<CrawlJob> CreateJobAsync(string url, int maxDepth)
    {
        var job = new CrawlJob
        {
            Id = Guid.NewGuid(),
            InputUrl = url,
            MaxDepth = Math.Max(1, Math.Min(maxDepth, 5)),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.CrawlJobs.Add(job);
        await _db.SaveChangesAsync();

        var correlationId = Guid.NewGuid();
        var message = new CrawlJobCreated(job.Id, job.InputUrl, job.MaxDepth, correlationId);

        await _publisher.PublishAsync(message, "crawl.events");

        var jobEvent = new JobEvent
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            EventType = "JobCreated",
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow,
            EventData = new Dictionary<string, object>
            {
                { "url", url },
                { "maxDepth", maxDepth }
            }
        };

        _db.JobEvents.Add(jobEvent);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created job {JobId} for URL {Url}", job.Id, url);

        return job;
    }

    public async Task<CrawlJob?> GetJobAsync(Guid jobId)
    {
        return await _db.CrawlJobs.FindAsync(jobId);
    }

    public async Task<(List<CrawlJob> Jobs, int Total)> GetJobHistoryAsync(int page, int pageSize)
    {
        var query = _db.CrawlJobs.OrderByDescending(j => j.CreatedAt);
        var total = await query.CountAsync();
        var jobs = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (jobs, total);
    }

    public async Task<JobDetailsDto> GetJobDetailsAsync(Guid jobId)
    {
        var job = await _db.CrawlJobs.FindAsync(jobId);
        if (job == null)
            throw new KeyNotFoundException($"Job {jobId} not found");

        var pages = await _db.CrawledPages
            .Where(p => p.JobId == jobId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        var pageHierarchy = BuildPageHierarchy(job.InputUrl, pages);

        return new JobDetailsDto
        {
            JobId = job.Id,
            InputUrl = job.InputUrl,
            Status = job.Status,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            TotalPagesFound = job.TotalPagesFound,
            Pages = pageHierarchy
        };
    }

    private List<PageNodeDto> BuildPageHierarchy(string rootUrl, List<CrawledPage> pages)
    {
        var pageMap = pages.ToDictionary(p => p.NormalizedUrl);
        var roots = pages.Where(p => p.Url == rootUrl || p.NormalizedUrl == SharedDomain.Utilities.UrlNormalizer.Normalize(rootUrl))
            .ToList();

        return roots.Select(root => BuildNode(root, pageMap, new HashSet<Guid>())).ToList();
    }

    private PageNodeDto BuildNode(CrawledPage page, Dictionary<string, CrawledPage> pageMap, HashSet<Guid> visited)
    {
        if (visited.Contains(page.Id))
            return new PageNodeDto { Url = page.Url, DomainLinkRatio = page.DomainLinkRatio };

        visited.Add(page.Id);

        return new PageNodeDto
        {
            Url = page.Url,
            Title = page.Title,
            DomainLinkRatio = page.DomainLinkRatio,
            OutgoingLinksCount = page.OutgoingLinksCount,
            InternalLinksCount = page.InternalLinksCount,
            Children = new List<PageNodeDto>()
        };
    }
}

public class JobDetailsDto
{
    public Guid JobId { get; set; }
    public string InputUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalPagesFound { get; set; }
    public List<PageNodeDto> Pages { get; set; } = new();
}

public class PageNodeDto
{
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public decimal? DomainLinkRatio { get; set; }
    public int OutgoingLinksCount { get; set; }
    public int InternalLinksCount { get; set; }
    public List<PageNodeDto> Children { get; set; } = new();
}

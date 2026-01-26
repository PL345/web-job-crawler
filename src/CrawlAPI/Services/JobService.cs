using CrawlAPI.Contracts.Jobs;
using CrawlAPI.Infrastructure;
using CrawlAPI.Services.Internal;
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
    Task<bool> CancelJobAsync(Guid jobId);
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
        url = ValidationHelper.NormalizeUrl(url);
        var normalizedDepth = ValidationHelper.NormalizeMaxDepth(maxDepth);

        var job = new CrawlJob
        {
            Id = Guid.NewGuid(),
            InputUrl = url,
            MaxDepth = normalizedDepth,
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
                { "maxDepth", normalizedDepth }
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

        return JobMapper.MapToJobDetailsDto(job, pages);
    }

    public async Task<bool> CancelJobAsync(Guid jobId)
    {
        var job = await _db.CrawlJobs.FindAsync(jobId);
        if (job == null)
            return false;

        if (job.Status != "Pending" && job.Status != "Running")
            return false;

        job.Status = "Cancelled";
        job.CompletedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;
        job.FailureReason = "Cancelled by user";

        await _db.SaveChangesAsync();

        _logger.LogInformation("Job {JobId} cancelled by user", jobId);
        return true;
    }
}

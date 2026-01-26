using Xunit;
using Microsoft.EntityFrameworkCore;
using CrawlWorker.Services;
using CrawlWorker.Infrastructure;
using SharedDomain.Models;
using SharedDomain.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CrawlAPI.Tests;

public class CrawlingServiceIntegrationTests : IAsyncLifetime
{
    private CrawlerDbContext _db = null!;
    private CrawlingService _crawlingService = null!;
    private Guid _testJobId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<CrawlerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new CrawlerDbContext(options);
        await _db.Database.EnsureCreatedAsync();

        // Create a mock logger
        var httpClientFactory = new System.Net.Http.HttpClientHandler
        {
            UseProxy = false,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };
        
        var httpClient = new System.Net.Http.HttpClient(httpClientFactory)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        var mockLogger = new MockLogger<CrawlingService>();
        _crawlingService = new CrawlingService(_db, mockLogger, httpClient);
    }

    public async Task DisposeAsync()
    {
        if (_db != null)
        {
            await _db.Database.EnsureDeletedAsync();
            _db.Dispose();
        }
    }

    [Fact]
    public async Task CrawlAsync_WithSimpleHtml_CreatesPageWithCorrectMetrics()
    {
        // Arrange
        var job = new CrawlJob
        {
            Id = _testJobId,
            InputUrl = "https://example.com",
            MaxDepth = 1,
            Status = "Running",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow
        };
        _db.CrawlJobs.Add(job);
        await _db.SaveChangesAsync();

        // Act - Crawl a real website
        await _crawlingService.CrawlAsync(_testJobId, "https://example.com", maxDepth: 1);

        // Assert
        var crawledPages = await _db.CrawledPages
            .Where(p => p.JobId == _testJobId)
            .ToListAsync();

        Assert.NotEmpty(crawledPages);
        
        var firstPage = crawledPages.First();
        Assert.Equal("https://example.com", firstPage.Url);
        Assert.Equal(200, firstPage.StatusCode);
        Assert.NotEmpty(firstPage.Title);
        Assert.True(firstPage.DomainLinkRatio >= 0 && firstPage.DomainLinkRatio <= 1);
        Assert.True(firstPage.OutgoingLinksCount >= 0);
    }

    [Fact]
    public async Task CrawlAsync_WithMultiplePages_RespectsDomainBoundary()
    {
        // Arrange
        var job = new CrawlJob
        {
            Id = _testJobId,
            InputUrl = "https://example.com",
            MaxDepth = 2,
            Status = "Running",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow
        };
        _db.CrawlJobs.Add(job);
        await _db.SaveChangesAsync();

        // Act
        await _crawlingService.CrawlAsync(_testJobId, "https://example.com", maxDepth: 2);

        // Assert - All pages should be from example.com domain
        var crawledPages = await _db.CrawledPages
            .Where(p => p.JobId == _testJobId)
            .ToListAsync();

        foreach (var page in crawledPages)
        {
            var pageDomain = UrlNormalizer.GetDomain(page.Url);
            var startDomain = UrlNormalizer.GetDomain("https://example.com");
            Assert.Equal(startDomain, pageDomain);
        }
    }

    [Fact]
    public async Task CrawlAsync_IsDuplicateUrlNormalized_PreventsDuplicateProcessing()
    {
        // Arrange
        var job = new CrawlJob
        {
            Id = _testJobId,
            InputUrl = "https://example.com",
            MaxDepth = 1,
            Status = "Running",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow
        };
        _db.CrawlJobs.Add(job);
        await _db.SaveChangesAsync();

        // Act - Insert a page first
        var page = new CrawledPage
        {
            Id = Guid.NewGuid(),
            JobId = _testJobId,
            Url = "https://example.com",
            NormalizedUrl = "https://example.com",
            Title = "Example",
            StatusCode = 200,
            DomainLinkRatio = 0.5m,
            OutgoingLinksCount = 1,
            InternalLinksCount = 1,
            CrawledAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.CrawledPages.Add(page);
        await _db.SaveChangesAsync();

        var countBefore = await _db.CrawledPages.CountAsync();

        // Act - Crawl again (should not create duplicate)
        await _crawlingService.CrawlAsync(_testJobId, "https://example.com", maxDepth: 1);

        // Assert - Should have same count (no duplicates)
        var countAfter = await _db.CrawledPages
            .Where(p => p.JobId == _testJobId)
            .CountAsync();

        Assert.Equal(1, countAfter);
    }

    [Fact]
    public async Task CrawlAsync_UpdatesJobStatusToCompleted()
    {
        // Arrange
        var job = new CrawlJob
        {
            Id = _testJobId,
            InputUrl = "https://example.com",
            MaxDepth = 1,
            Status = "Running",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow
        };
        _db.CrawlJobs.Add(job);
        await _db.SaveChangesAsync();

        // Act
        await _crawlingService.CrawlAsync(_testJobId, "https://example.com", maxDepth: 1);

        // Assert
        var updatedJob = await _db.CrawlJobs.FindAsync(_testJobId);
        Assert.Equal("Completed", updatedJob.Status);
        Assert.NotNull(updatedJob.CompletedAt);
        Assert.True(updatedJob.TotalPagesFound > 0);
    }

    [Fact]
    public async Task CrawlAsync_WithInvalidUrl_FailsJobGracefully()
    {
        // Arrange
        var job = new CrawlJob
        {
            Id = _testJobId,
            InputUrl = "https://invalid-domain-that-does-not-exist-xyzabc.com",
            MaxDepth = 1,
            Status = "Running",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow
        };
        _db.CrawlJobs.Add(job);
        await _db.SaveChangesAsync();

        // Act
        await _crawlingService.CrawlAsync(_testJobId, "https://invalid-domain-that-does-not-exist-xyzabc.com", maxDepth: 1);

        // Assert - Job should be marked Failed
        var updatedJob = await _db.CrawlJobs.FindAsync(_testJobId);
        Assert.Equal("Failed", updatedJob.Status);
        Assert.NotNull(updatedJob.FailureReason);
        Assert.Contains("unreachable", updatedJob.FailureReason.ToLower());
    }

    [Fact]
    public async Task CrawlAsync_CalculatesDomainLinkRatioCorrectly()
    {
        // Arrange - Setup a job
        var job = new CrawlJob
        {
            Id = _testJobId,
            InputUrl = "https://example.com",
            MaxDepth = 1,
            Status = "Running",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow
        };
        _db.CrawlJobs.Add(job);
        await _db.SaveChangesAsync();

        // Act - Crawl
        await _crawlingService.CrawlAsync(_testJobId, "https://example.com", maxDepth: 1);

        // Assert - Domain Link Ratio should be between 0 and 1
        var crawledPages = await _db.CrawledPages
            .Where(p => p.JobId == _testJobId)
            .ToListAsync();

        foreach (var page in crawledPages)
        {
            if (page.OutgoingLinksCount > 0)
            {
                Assert.True(page.DomainLinkRatio >= 0, $"DomainLinkRatio should be >= 0, got {page.DomainLinkRatio}");
                Assert.True(page.DomainLinkRatio <= 1, $"DomainLinkRatio should be <= 1, got {page.DomainLinkRatio}");
                Assert.True(page.InternalLinksCount <= page.OutgoingLinksCount, 
                    "Internal links should not exceed total links");
            }
            else
            {
                // If no outgoing links, ratio should be 0
                Assert.Equal(0m, page.DomainLinkRatio);
            }
        }
    }
}

/// <summary>
/// Mock logger for testing
/// </summary>
public class MockLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => null;

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        => true;

    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel,
        Microsoft.Extensions.Logging.EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        // Silently log to allow test execution
    }
}

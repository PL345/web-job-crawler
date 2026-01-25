namespace SharedDomain.Models;

public class CrawlJob
{
    public Guid Id { get; set; }
    public string InputUrl { get; set; } = string.Empty;
    public int MaxDepth { get; set; } = 2;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
    public int TotalPagesFound { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // New properties - will be added by migration
    public string? CurrentUrl { get; set; }
    public int PagesProcessed { get; set; }
}

public class CrawledPage
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string NormalizedUrl { get; set; } = string.Empty;
    public string? Title { get; set; }
    public int? StatusCode { get; set; }
    public decimal? DomainLinkRatio { get; set; }
    public int OutgoingLinksCount { get; set; }
    public int InternalLinksCount { get; set; }
    public DateTime? CrawledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PageLink
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid SourcePageId { get; set; }
    public string TargetUrl { get; set; } = string.Empty;
    public string? LinkText { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class JobEvent
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Dictionary<string, object>? EventData { get; set; }
    public Guid? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
}

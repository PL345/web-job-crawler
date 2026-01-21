namespace SharedDomain.Messages;

public record CrawlJobCreated(
    Guid JobId,
    string InputUrl,
    int MaxDepth,
    Guid CorrelationId
);

public record PageDiscovered(
    Guid JobId,
    string Url,
    int Depth,
    Guid CorrelationId
);

public record PageCrawlCompleted(
    Guid JobId,
    string Url,
    int StatusCode,
    List<string> DiscoveredLinks,
    Guid CorrelationId
);

public record JobCompleted(
    Guid JobId,
    int TotalPagesCrawled,
    Guid CorrelationId
);

public record CrawlJobFailed(
    Guid JobId,
    string FailureReason,
    Guid CorrelationId
);

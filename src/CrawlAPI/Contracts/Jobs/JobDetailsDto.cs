using SharedDomain.Models;

namespace CrawlAPI.Contracts.Jobs;

public class JobDetailsDto
{
    public Guid JobId { get; set; }
    public string InputUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalPagesFound { get; set; }
    public string? CurrentUrl { get; set; }
    public int PagesProcessed { get; set; }
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

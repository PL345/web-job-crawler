using CrawlAPI.Contracts.Jobs;
using SharedDomain.Models;

namespace CrawlAPI.Services.Internal;

/// <summary>
/// Handles conversion from domain models to DTOs.
/// </summary>
internal static class JobMapper
{
    public static JobDetailsDto MapToJobDetailsDto(CrawlJob job, List<CrawledPage> pages)
    {
        var pageNodes = pages
            .Select(p => new PageNodeDto
            {
                Url = p.Url,
                Title = p.Title,
                DomainLinkRatio = p.DomainLinkRatio,
                OutgoingLinksCount = p.OutgoingLinksCount,
                InternalLinksCount = p.InternalLinksCount,
                Children = new List<PageNodeDto>()
            })
            .ToList();

        return new JobDetailsDto
        {
            JobId = job.Id,
            InputUrl = job.InputUrl,
            Status = job.Status,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            TotalPagesFound = job.TotalPagesFound,
            CurrentUrl = job.CurrentUrl,
            PagesProcessed = job.PagesProcessed,
            Pages = pageNodes
        };
    }

    public static JobHistoryResponse MapToJobHistoryResponse(List<CrawlJob> jobs, int total, int page, int pageSize)
    {
        return new JobHistoryResponse
        {
            Jobs = jobs,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = (total + pageSize - 1) / pageSize
        };
    }
}

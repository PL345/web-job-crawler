using System.Text.Json.Serialization;
using SharedDomain.Models;

namespace CrawlAPI.Contracts.Jobs;

public class JobHistoryResponse
{
    [JsonPropertyName("jobs")]
    public List<CrawlJob> Jobs { get; set; } = new();

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }
}

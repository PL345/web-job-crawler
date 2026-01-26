namespace CrawlAPI.Contracts.Jobs;

public class CreateJobRequest
{
    public string Url { get; set; } = string.Empty;
    public int? MaxDepth { get; set; }
}

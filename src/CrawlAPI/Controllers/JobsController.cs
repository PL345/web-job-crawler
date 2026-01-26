using CrawlAPI.Contracts.Jobs;
using CrawlAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrawlAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IJobService jobService, ILogger<JobsController> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest(new { error = "URL is required" });

        try
        {
            var job = await _jobService.CreateJobAsync(request.Url, request.MaxDepth ?? 2);
            return Ok(new { jobId = job.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("{jobId}")]
    public async Task<IActionResult> GetJob(Guid jobId)
    {
        var job = await _jobService.GetJobAsync(jobId);
        if (job == null)
            return NotFound(new { error = "Job not found" });

        return Ok(job);
    }

    [HttpGet("{jobId}/details")]
    public async Task<IActionResult> GetJobDetails(Guid jobId)
    {
        try
        {
            var details = await _jobService.GetJobDetailsAsync(jobId);
            return Ok(details);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Job not found" });
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (jobs, total) = await _jobService.GetJobHistoryAsync(page, pageSize);
        var response = new JobHistoryResponse
        {
            Jobs = jobs,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = (total + pageSize - 1) / pageSize
        };

        return Ok(response);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    [HttpPost("{jobId}/cancel")]
    public async Task<IActionResult> CancelJob(Guid jobId)
    {
        try
        {
            var success = await _jobService.CancelJobAsync(jobId);
            if (!success)
                return NotFound(new { error = "Job not found or cannot be cancelled" });

            return Ok(new { message = "Job cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling job {JobId}", jobId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

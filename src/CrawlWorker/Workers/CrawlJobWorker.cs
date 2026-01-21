using SharedDomain.Messages;
using CrawlWorker.Infrastructure;
using CrawlWorker.Services;

namespace CrawlWorker.Workers;

public class CrawlJobWorker : BackgroundService
{
    private readonly ILogger<CrawlJobWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageConsumer _messageConsumer;

    public CrawlJobWorker(ILogger<CrawlJobWorker> logger, IServiceProvider serviceProvider, IMessageConsumer messageConsumer)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _messageConsumer = messageConsumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Crawl job worker starting");

        _messageConsumer.SubscribeToJobCreated(HandleJobCreatedAsync);
        _messageConsumer.Start();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("Crawl job worker stopping");
    }

    private async Task HandleJobCreatedAsync(CrawlJobCreated message)
    {
        _logger.LogInformation("Processing job {JobId} for URL {Url}", message.JobId, message.InputUrl);

        using (var scope = _serviceProvider.CreateScope())
        {
            var crawlingService = scope.ServiceProvider.GetRequiredService<ICrawlingService>();

            try
            {
                await crawlingService.CrawlAsync(message.JobId, message.InputUrl, message.MaxDepth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing job {JobId}", message.JobId);
            }
        }
    }
}

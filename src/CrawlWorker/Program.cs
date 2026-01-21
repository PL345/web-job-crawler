using CrawlWorker.Infrastructure;
using CrawlWorker.Services;
using CrawlWorker.Workers;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

var builder = Host.CreateDefaultBuilder(args)
    .UseSerilog()
    .ConfigureServices((context, services) =>
    {
        var dbConnectionString = context.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<CrawlerDbContext>(options =>
            options.UseNpgsql(dbConnectionString));

        var rabbitMqHost = context.Configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitMqPort = int.Parse(context.Configuration["RabbitMQ:Port"] ?? "5672");
        var factory = new ConnectionFactory
        {
            HostName = rabbitMqHost,
            Port = rabbitMqPort
        };

        services.AddSingleton<IConnection>(factory.CreateConnection());
        services.AddSingleton<IMessageConsumer, RabbitMqConsumer>();

        services.AddHttpClient<ICrawlingService, CrawlingService>();

        services.AddHostedService<CrawlJobWorker>();
    });

var host = builder.Build();

await host.RunAsync();

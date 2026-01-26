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
        var rabbitMqUsername = context.Configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitMqPassword = context.Configuration["RabbitMQ:Password"] ?? "guest";
        var factory = new ConnectionFactory
        {
            HostName = rabbitMqHost,
            Port = rabbitMqPort,
            UserName = rabbitMqUsername,
            Password = rabbitMqPassword
        };

        services.AddSingleton<IConnection>(factory.CreateConnection());
        services.AddSingleton<IMessageConsumer, RabbitMqConsumer>();

        // Configure HttpClient with proper handler to bypass system proxy issues in containerized environments
        services.AddHttpClient<ICrawlingService, CrawlingService>()
            .ConfigureHttpClient((client) =>
            {
                // Already configured in CrawlingService constructor
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler
                {
                    // Bypass system proxy detection which can cause issues in containerized environments
                    UseProxy = false,
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 5,
                    // Enable automatic decompression for gzip/deflate
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                return handler;
            });

        services.AddHostedService<CrawlJobWorker>();
    });

var host = builder.Build();

await host.RunAsync();

using CrawlAPI.Infrastructure;
using CrawlAPI.Services;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<CrawlerDbContext>(options =>
    options.UseNpgsql(dbConnectionString));

// Try to connect to RabbitMQ, but don't fail if it's not available
try
{
    var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
    var rabbitMqPort = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672");
    var rabbitMqUsername = builder.Configuration["RabbitMQ:Username"] ?? "guest";
    var rabbitMqPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";
    var factory = new ConnectionFactory
    {
        HostName = rabbitMqHost,
        Port = rabbitMqPort,
        UserName = rabbitMqUsername,
        Password = rabbitMqPassword
    };
    builder.Services.AddSingleton<IConnection>(factory.CreateConnection());
    Log.Information("Connected to RabbitMQ successfully");
}
catch (Exception ex)
{
    Log.Warning("Could not connect to RabbitMQ: {Error}. Running without message queue.", ex.Message);
    builder.Services.AddSingleton<IConnection>((IConnection)null!);
}

builder.Services.AddScoped<IMessagePublisher, RabbitMqPublisher>();
builder.Services.AddScoped<IJobService, JobService>();

var app = builder.Build();

// Apply EF migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CrawlerDbContext>();
    
    try
    {
        db.Database.Migrate();
        Log.Information("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        Log.Warning("Could not apply database migrations: {Error}", ex.Message);
        // Fallback to EnsureCreated for development
        db.Database.EnsureCreated();
    }
}

app.UseSwagger(c => c.RouteTemplate = "swagger/{documentName}/swagger.json");
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CrawlAPI v1");
    c.RoutePrefix = "swagger";
});

// CORS middleware handles preflight requests automatically
app.UseCors("AllowAll");

app.UseAuthorization();
app.MapControllers();

app.Run();
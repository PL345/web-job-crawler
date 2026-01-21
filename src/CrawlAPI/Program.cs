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

var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitMqPort = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672");
var factory = new ConnectionFactory
{
    HostName = rabbitMqHost,
    Port = rabbitMqPort
};
builder.Services.AddSingleton<IConnection>(factory.CreateConnection());

builder.Services.AddScoped<IMessagePublisher, RabbitMqPublisher>();
builder.Services.AddScoped<IJobService, JobService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

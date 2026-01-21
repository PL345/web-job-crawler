using Microsoft.EntityFrameworkCore;
using SharedDomain.Models;
using System.Text.Json;

namespace CrawlWorker.Infrastructure;

public class CrawlerDbContext : DbContext
{
    public DbSet<CrawlJob> CrawlJobs { get; set; }
    public DbSet<CrawledPage> CrawledPages { get; set; }
    public DbSet<PageLink> PageLinks { get; set; }
    public DbSet<JobEvent> JobEvents { get; set; }

    public CrawlerDbContext(DbContextOptions<CrawlerDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CrawlJob>()
            .HasKey(j => j.Id);

        modelBuilder.Entity<CrawledPage>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<CrawledPage>()
            .HasIndex(p => new { p.JobId, p.NormalizedUrl })
            .IsUnique();

        modelBuilder.Entity<PageLink>()
            .HasKey(l => l.Id);

        modelBuilder.Entity<PageLink>()
            .HasIndex(l => new { l.SourcePageId, l.TargetUrl })
            .IsUnique();

        modelBuilder.Entity<JobEvent>()
            .HasKey(e => e.Id);

        // Configure EventData as JSON
        modelBuilder.Entity<JobEvent>()
            .Property(e => e.EventData)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));
    }
}

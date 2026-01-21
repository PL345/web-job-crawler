using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CrawlAPI.Infrastructure;

public class CrawlerDbContextFactory : IDesignTimeDbContextFactory<CrawlerDbContext>
{
    public CrawlerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CrawlerDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=webcrawler;Username=crawler;Password=crawler_password");

        return new CrawlerDbContext(optionsBuilder.Options);
    }
}

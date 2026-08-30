using Microsoft.EntityFrameworkCore;

namespace HomePlatform.Infrastructure.Persistence;

public sealed class HomePlatformDbContext(
    DbContextOptions<HomePlatformDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(HomePlatformDbContext).Assembly);
    }
}
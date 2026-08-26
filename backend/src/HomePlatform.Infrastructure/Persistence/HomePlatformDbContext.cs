using Microsoft.EntityFrameworkCore;

namespace HomePlatform.Infrastructure.Persistence;

public sealed class HomePlatformDbContext(DbContextOptions<HomePlatformDbContext> options)
    : DbContext(options);

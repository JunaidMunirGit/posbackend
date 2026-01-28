using Microsoft.EntityFrameworkCore;
using Pos.Infrastructure.Abstractions.Persistence;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Product> Products => Set<Product>();

}
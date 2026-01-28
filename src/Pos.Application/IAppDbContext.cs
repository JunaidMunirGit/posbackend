using Microsoft.EntityFrameworkCore;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Abstractions.Persistence
{
    public interface IAppDbContext
    {
        DbSet<Product> Products { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
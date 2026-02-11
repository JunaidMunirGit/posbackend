using Microsoft.EntityFrameworkCore;
using Pos.Domain.Entities;
using Pos.Domain.Security;

namespace Pos.Application.Common.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Role> Roles { get; }
        DbSet<UserRole> UserRoles { get; }
        DbSet<Product> Products { get; }
        DbSet<PasswordResetToken> PasswordResetTokens { get; }


        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
using Pos.Domain.Security;
using Pos.Infrastructure.Persistence;

namespace Pos.Infrastructure.Persistence.Seed;

public static class PermissionSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Roles.Any()) return;

        var admin = new Role
        {
            Name = "Admin",
            Permissions = Enum.GetValues<Permission>()
                .Select(p => new RolePermission { Permission = p })
                .ToList()
        };

        var cashier = new Role
        {
            Name = "Cashier",
            Permissions = new[]
            {
                Permission.ViewProducts,
                Permission.CreateSale
            }.Select(p => new RolePermission { Permission = p }).ToList()
        };

        db.Roles.AddRange(admin, cashier);
        db.SaveChanges();
    }
}

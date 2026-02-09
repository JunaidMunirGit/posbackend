using MediatR;
using Pos.Application.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Features.Auth.Commands.AssignRoleCommand
{
    public class AssignRoleCommandHandler(AppDbContext db) : IRequestHandler<AssignRoleCommand, bool>
    {
        public async Task<bool> Handle(AssignRoleCommand request, CancellationToken ct)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var roleName = request.RoleName.Trim();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email!.ToLower() == email, ct);
            if (user is null) throw new NotFoundException("User not found.");

            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);
            if (role is null) throw new NotFoundException("Role not found.");

            var exists = await db.UserRoles.AnyAsync(x => x.UserId == user.Id && x.RoleId == role.Id, ct);
            if (exists) return true;

            db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });

            await db.SaveChangesAsync(ct);
            return true;
        }
    }
}

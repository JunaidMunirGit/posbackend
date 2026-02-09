using MediatR;
using Pos.Application.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using Pos.Domain.Security;
using Pos.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace Pos.Application.Features.Auth.Commands.AssignRoleCommand
{
    public class AssignRoleCommandHandler(IAppDbContext db) : IRequestHandler<AssignRoleCommand, bool>
    {
        public async Task<bool> Handle(AssignRoleCommand request, CancellationToken ct)
        {
            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
                ?? throw new NotFoundException("User not found.");

            var role = await db.Roles
                .FirstOrDefaultAsync(r => r.Id == request.RoleId, ct)
                ?? throw new NotFoundException("Role not found.");

            var exists = await db.UserRoles
                .AnyAsync(x => x.UserId == user.Id && x.RoleId == role.Id, ct);

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
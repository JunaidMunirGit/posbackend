using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Features.Auth.Dtos
{
    public sealed class AssignRoleRequest
    {
        public required int UserId { get; init; }
        public required int RoleId { get; init; }
    }
}
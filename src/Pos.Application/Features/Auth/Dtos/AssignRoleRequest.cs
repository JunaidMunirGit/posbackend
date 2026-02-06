using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Features.Auth.Dtos
{
    public class AssignRoleRequest
    {
        public record AssignRoleRequest(string Email, string RoleName);
    }
}

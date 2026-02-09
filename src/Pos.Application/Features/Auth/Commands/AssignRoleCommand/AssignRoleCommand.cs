using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Features.Auth.Commands.AssignRoleCommand
{
    public record AssignRoleCommand(int UserId, int RoleId) : IRequest<bool>;
}

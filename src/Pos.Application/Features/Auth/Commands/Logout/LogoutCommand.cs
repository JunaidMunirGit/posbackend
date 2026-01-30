using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Features.Auth.Commands.Logout
{
    public record LogoutCommand(string? RawRefreshToken) : IRequest;
}

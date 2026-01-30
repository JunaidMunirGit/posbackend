using MediatR;
using Pos.Application.Abstractions.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Features.Auth.Commands.Logout
{
    public class LogoutHandler(IAuthService auth) : IRequestHandler<LogoutCommand>
    {
        public async Task Handle(LogoutCommand cmd, CancellationToken ct)
            => await auth.LogoutAsync(cmd.RawRefreshToken, ct);
    }
}

using MediatR;
using Pos.Application.Abstractions.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Features.Auth.Commands.Logout
{
    public class LogoutCommandHandler(IAuthService auth) : IRequestHandler<LogoutCommand>
    {
        public async Task Handle(LogoutCommand cmd, CancellationToken cancellationToken)
            => await auth.LogoutAsync(cmd.RawRefreshToken, cancellationToken);
    }
}

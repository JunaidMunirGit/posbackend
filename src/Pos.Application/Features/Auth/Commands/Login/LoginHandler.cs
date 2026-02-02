using MediatR;
using Pos.Application.Abstractions.Security;
using Pos.Application.Features.Auth.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Features.Auth.Commands.Login
{
    public class LoginHandler(IAuthService auth) : IRequestHandler<LoginCommand, AuthResponse>
    {
        public Task<AuthResponse> Handle(LoginCommand cmd, CancellationToken cancellationToken)
            => auth.LoginAsync(cmd.Request, cancellationToken);
    }
}
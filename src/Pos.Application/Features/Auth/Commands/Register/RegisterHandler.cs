using MediatR;
using Pos.Application.Abstractions;
using Pos.Application.Abstractions.Security;
using Pos.Application.Features.Auth.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Features.Auth.Commands.Register
{
    public class RegisterHandler : IRequestHandler<RegisterCommand, AuthResponse>
    {
        private readonly IAuthService _auth;
        public RegisterHandler(IAuthService auth) => _auth = auth;

        public Task<AuthResponse> Handle(RegisterCommand cmd, CancellationToken ct)
            => _auth.RegisterAsync(cmd.Request, ct);
    }
}

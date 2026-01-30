using MediatR;
using Pos.Application.Abstractions.Security;
using Pos.Application.Features.Auth.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Features.Auth.Commands.RefreshToken
{
    public class RefreshTokenHandler(IAuthService auth) : IRequestHandler<RefreshTokenCommand, RefreshResponse>
    {
        public Task<RefreshResponse> Handle(RefreshTokenCommand cmd, CancellationToken ct)
            => auth.RefreshAsync(cmd.RawRefreshToken, ct);
    }
}

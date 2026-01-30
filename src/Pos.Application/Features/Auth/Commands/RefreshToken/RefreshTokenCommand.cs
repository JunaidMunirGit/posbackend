using MediatR;
using Pos.Application.Features.Auth.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Features.Auth.Commands.RefreshToken
{
    public record RefreshTokenCommand(string RawRefreshToken) : IRequest<RefreshResponse>;
}

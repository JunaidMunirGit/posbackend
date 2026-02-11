using MediatR;
using Pos.Application.Features.Auth.Dtos;

namespace Pos.Application.Features.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest<ForgotPasswordResult>;
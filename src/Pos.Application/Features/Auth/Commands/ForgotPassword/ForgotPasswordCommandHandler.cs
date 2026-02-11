using MediatR;
using Pos.Application.Abstractions.Security;
using Pos.Application.Features.Auth.Dtos;

namespace Pos.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler(IAuthService auth) : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    public Task<ForgotPasswordResult> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        => auth.ForgotPasswordAsync(request.Email, cancellationToken);
}
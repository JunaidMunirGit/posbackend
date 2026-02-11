using MediatR;
using Pos.Application.Abstractions.Security;

namespace Pos.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler(IAuthService auth)
    : IRequestHandler<ResetPasswordCommand, bool>
{
    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        await auth.ResetPasswordAsync(request.Token, request.NewPassword, cancellationToken);
        return true;
    }
}
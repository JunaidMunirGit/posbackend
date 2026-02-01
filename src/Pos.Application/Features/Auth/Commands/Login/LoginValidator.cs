using FluentValidation;

namespace Pos.Application.Features.Auth.Commands.Login;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Request.Email)
            .NotEmpty().EmailAddress().MaximumLength(200);

        RuleFor(x => x.Request.Password)
            .NotEmpty();
    }
}
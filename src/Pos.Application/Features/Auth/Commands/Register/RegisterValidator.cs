using FluentValidation;

namespace Pos.Application.Features.Auth.Commands.Register;

public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Request.Email)
            .NotEmpty().EmailAddress().MaximumLength(200);

        RuleFor(x => x.Request.Password)
            .NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}
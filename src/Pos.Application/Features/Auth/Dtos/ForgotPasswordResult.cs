using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Features.Auth.Dtos
{
    public record ForgotPasswordResult(bool Ok, string? ResetToken);
}
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Features.Auth.Dtos
{
    public class LoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Auth.Dtos
{
    public class RegisterRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
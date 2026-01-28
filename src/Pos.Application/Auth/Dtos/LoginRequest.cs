using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Auth.Dtos
{
    public class LoginRequest
    {
        public required string Email { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Auth.Dtos
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = default!;
        public DateTime AccessTokenExpiresDate { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Features.Auth.Dtos
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = default!;
        public DateTime AccessTokenExpiresDate { get; set; }
        public string? RefreshToken { get; set; }
    }
}
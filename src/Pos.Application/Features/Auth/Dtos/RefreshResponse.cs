using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Features.Auth.Dtos
{
    public class RefreshResponse
    {
        public required string AccessToken { get; set; }
        public DateTime AccessTokenExpiresDate { get; set; }
        public string? RefreshToken { get; set; }

    }
}
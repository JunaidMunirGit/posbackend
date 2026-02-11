using Pos.Application.Features.Auth.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Abstractions.Security
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest req, CancellationToken ct);
        Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct);
        Task<RefreshResponse> RefreshAsync(string rawRefreshToken, CancellationToken ct);
        Task LogoutAsync(string? rawRefreshToken, CancellationToken ct);
        Task<ForgotPasswordResult> ForgotPasswordAsync(string email, CancellationToken ct);
        Task ResetPasswordAsync(string token, string newPassword, CancellationToken ct);
    }
}
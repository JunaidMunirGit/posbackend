using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Pos.Application.Abstractions.Security;
using Pos.Application.Features.Auth.Dtos;
using Pos.Domain.Entities;
using Pos.Domain.Enums;
using Pos.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Pos.Infrastructure.Security;

public class AuthService(AppDbContext db, IPasswordHasher hasher, IConfiguration config) : IAuthService
{
    private static readonly TimeSpan AccessTokenTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(30);

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();

        var exists = await db.Users.AnyAsync(u => u.Email.ToLower() == email, ct);
        if (exists)
            throw new InvalidOperationException("Unable to register with provided credentials.");

        var user = new User
        {
            Email = email,
            Status = UserStatus.Active,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            IsActive = true
        };
        user.PasswordHash = hasher.Hash(req.Password);


        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var (accessToken, expiresAt) = CreateAccessToken(user);

        var rawRefresh = TokenHelper.GenerateRefreshToken();
        var refreshHash = TokenHelper.Sha256(rawRefresh);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            CreatedDate = DateTime.UtcNow,
            ExpiresDate = DateTime.UtcNow.Add(RefreshTokenTtl)
        });

        await db.SaveChangesAsync(ct);

        return new AuthResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresDate = expiresAt,
            RefreshToken = rawRefresh // return to controller to set cookie
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var verify = hasher.Hash(req.Password);
        if (!hasher.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");


        var (accessToken, expiresAt) = CreateAccessToken(user);

        var rawRefresh = TokenHelper.GenerateRefreshToken();
        var refreshHash = TokenHelper.Sha256(rawRefresh);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            CreatedDate = DateTime.UtcNow,
            ExpiresDate = DateTime.UtcNow.Add(RefreshTokenTtl)
        });

        await db.SaveChangesAsync(ct);

        return new AuthResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresDate = expiresAt,
            RefreshToken = rawRefresh
        };
    }

    public async Task<RefreshResponse> RefreshAsync(string rawRefreshToken, CancellationToken ct)
    {
        var refreshHash = TokenHelper.Sha256(rawRefreshToken);

        var token = await db.RefreshTokens.AsTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == refreshHash, ct);

        if (token == null || !token.IsActive)
            throw new UnauthorizedAccessException("Invalid session.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == token.UserId, ct);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid session.");

        // rotate
        token.RevokedDate = DateTime.UtcNow;

        var newRaw = TokenHelper.GenerateRefreshToken();
        var newHash = TokenHelper.Sha256(newRaw);
        token.ReplacedByTokenHash = newHash;

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newHash,
            CreatedDate = DateTime.UtcNow,
            ExpiresDate = DateTime.UtcNow.Add(RefreshTokenTtl)
        });

        var (accessToken, expiresAt) = CreateAccessToken(user);
        await db.SaveChangesAsync(ct);

        return new RefreshResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresDate = expiresAt,
            RefreshToken = newRaw
        };
    }

    public async Task LogoutAsync(string? rawRefreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken)) return;

        var refreshHash = TokenHelper.Sha256(rawRefreshToken);
        var token = await db.RefreshTokens.AsTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == refreshHash, ct);

        if (token != null && token.IsActive)
        {
            token.RevokedDate = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    private (string token, DateTime expiresAtUtc) CreateAccessToken(User user)
    {
        var jwtKey = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
        var jwtIssuer = config["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer missing");
        var jwtAudience = config["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience missing");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? "")

        };

        claims.Add(new Claim(ClaimTypes.Role, user.Role.ToString()));


        var expires = DateTime.UtcNow.Add(AccessTokenTtl);

        var jwt = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }
}

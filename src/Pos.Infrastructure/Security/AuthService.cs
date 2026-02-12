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
    private const string PermissionClaimType = "permission";


    public async Task<AuthResponse> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();

        var exists = await db.Users.AnyAsync(u => u.Email.ToLower() == email, ct);
        if (exists)
            throw new InvalidOperationException("Unable to register with provided credentials.");

        var user = new User
        {
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            PhoneNumber = req.PhoneNumber.Trim(),
            Email = email,
            Status = UserStatus.Active,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            IsActive = true
        };
        user.PasswordHash = hasher.Hash(req.Password);


        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var (accessToken, expiresAt) = await CreateAccessTokenAsync(user, ct);

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

        if (!hasher.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");


        var (accessToken, expiresAt) = await CreateAccessTokenAsync(user, ct);

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

        if (token?.IsActive != true)
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

        var (accessToken, expiresAt) = await CreateAccessTokenAsync(user, ct);
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

        if (token?.IsActive == true)
        {
            token.RevokedDate = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<(string token, DateTime expiresAtUtc)> CreateAccessTokenAsync(User user, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(user);

        var jwtKey = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
        var jwtIssuer = config["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer missing");
        var jwtAudience = config["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience missing");

        var now = DateTime.UtcNow;
        var expires = now.Add(AccessTokenTtl);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        // Pull permissions from roles
        var permissions = await db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .SelectMany(ur => ur.Role.Permissions)
            .Select(rp => rp.Permission)
            .Distinct()
            .ToListAsync(ct);

        foreach (var permission in permissions)
            claims.Add(new Claim(PermissionClaimType, permission.ToString()));


        // If user can have multiple roles, add multiple ClaimTypes.Role claims instead.
        claims.Add(new Claim(ClaimTypes.Role, user.Role.ToString()));

        var jwt = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds
        );

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return (token, expires);
    }

    public async Task<ForgotPasswordResult> ForgotPasswordAsync(string email, CancellationToken ct)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email!.ToLower() == normalizedEmail, ct);

        // SECURITY: never reveal if email exists
        if (user is null)
            return new ForgotPasswordResult(true, null);

        var raw = TokenHelper.GenerateRefreshToken();
        var hash = TokenHelper.Sha256(raw);

        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = hash,
            CreatedDate = DateTime.UtcNow,
            ExpiresDate = DateTime.UtcNow.AddMinutes(30)
        });

        await db.SaveChangesAsync(ct);

        // DEV ONLY: return raw token
        return new ForgotPasswordResult(true, raw);
    }

    public async Task ResetPasswordAsync(string token, string newPassword, CancellationToken ct)
    {
        var hash = TokenHelper.Sha256(token);

        var reset = await db.PasswordResetTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == hash, ct);

        if (reset is null || reset.UsedAt != null || reset.ExpiresDate < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired reset token.");

        reset.UsedAt = DateTime.UtcNow;

        reset.User.PasswordHash = hasher.Hash(newPassword);
        reset.User.UpdatedDate = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pos.Application.Auth.Dtos;
using Pos.Domain.Entities;
using Pos.Domain.Enums;
using Pos.Infrastructure.Persistence;
using Pos.Infrastructure.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Pos.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher<User> _hasher;
        private readonly IConfiguration _config;

        private static readonly TimeSpan AccessTokenTtl = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(30);


        public AuthController(AppDbContext db, IPasswordHasher<User> hasher, IConfiguration config)
        {
            _db = db;
            _hasher = hasher;
            _config = config;
        }



        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req, CancellationToken ct)
        {
            var email = req.Email.Trim().ToLowerInvariant();

            var exists = await _db.Users.AnyAsync(u => u.Email.ToLower() == email, ct);
            if (exists)
            {
                return BadRequest(new { error = "Unable to register with provided credentials." });
            }

            var user = new User
            {
                Email = email,
                PasswordHash = _hasher.HashPassword(null!, req.Password),
                Status = UserStatus.Active,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                IsActive = true
            };

            user.PasswordHash = _hasher.HashPassword(user, req.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);

            var (accessToken, expiresAt) = CreateAccessToken(user);

            // create refresh token record (hashed)
            var rawRefresh = TokenHelper.GenerateRefreshToken();
            var refreshHash = TokenHelper.Sha256(rawRefresh);

            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshHash,
                CreatedDate = DateTime.UtcNow,
                ExpiresDate = DateTime.UtcNow.Add(RefreshTokenTtl)
            });

            await _db.SaveChangesAsync(ct);

            SetRefreshCookie(rawRefresh);

            return Ok(new AuthResponse
            {
                AccessToken = accessToken,
                AccessTokenExpiresDate = expiresAt
            });
        }


        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
        {
            var email = req.Email.Trim().ToLowerInvariant();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);

            // Safe error message: don't reveal if user exists
            if (user == null)
                return Unauthorized(new { error = "Invalid credentials." });

            var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
            if (verify == PasswordVerificationResult.Failed)
                return Unauthorized(new { error = "Invalid credentials." });

            // Optional: block inactive users if you have status fields
            // if (!user.IsActive) return Unauthorized(new { error = "Invalid credentials." });

            var (accessToken, expiresAt) = CreateAccessToken(user);

            // rotate: create new refresh token
            var rawRefresh = TokenHelper.GenerateRefreshToken();
            var refreshHash = TokenHelper.Sha256(rawRefresh);

            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshHash,
                CreatedDate = DateTime.UtcNow,
                ExpiresDate = DateTime.UtcNow.Add(RefreshTokenTtl)
            });

            await _db.SaveChangesAsync(ct);

            SetRefreshCookie(rawRefresh);

            return Ok(new AuthResponse
            {
                AccessToken = accessToken,
                AccessTokenExpiresDate = expiresAt
            });
        }



        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<RefreshResponse>> Refresh(CancellationToken ct)
        {
            var rawRefresh = Request.Cookies["refresh_token"];
            if (string.IsNullOrWhiteSpace(rawRefresh))
                return Unauthorized(new { error = "Invalid session." });

            var refreshHash = TokenHelper.Sha256(rawRefresh);

            var token = await _db.RefreshTokens
                .AsTracking()
                .FirstOrDefaultAsync(t => t.TokenHash == refreshHash, ct);

            if (token == null || !token.IsActive)
                return Unauthorized(new { error = "Invalid session." });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == token.UserId, ct);
            if (user == null)
                return Unauthorized(new { error = "Invalid session." });

            // Rotate refresh token: revoke old, create new
            token.RevokedDate = DateTime.UtcNow;

            var newRaw = TokenHelper.GenerateRefreshToken();
            var newHash = TokenHelper.Sha256(newRaw);
            token.ReplacedByTokenHash = newHash;

            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = newHash,
                CreatedDate = DateTime.UtcNow,
                ExpiresDate = DateTime.UtcNow.Add(RefreshTokenTtl)
            });

            var (accessToken, expiresAt) = CreateAccessToken(user);

            await _db.SaveChangesAsync(ct);

            SetRefreshCookie(newRaw);

            return Ok(new RefreshResponse
            {
                AccessToken = accessToken,
                AccessTokenExpiresDate = expiresAt
            });
        }



        [HttpPost("logout")]
        [Authorize] // must be authenticated to logout
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            // revoke current refresh token if present
            var rawRefresh = Request.Cookies["refresh_token"];
            if (!string.IsNullOrWhiteSpace(rawRefresh))
            {
                var refreshHash = TokenHelper.Sha256(rawRefresh);
                var token = await _db.RefreshTokens.AsTracking()
                    .FirstOrDefaultAsync(t => t.TokenHash == refreshHash, ct);

                if (token != null && token.IsActive)
                {
                    token.RevokedDate = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                }
            }

            Response.Cookies.Delete("refresh_token", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/auth"
            });

            return Ok(new { ok = true });
        }


        private (string token, DateTime expiresAtUtc) CreateAccessToken(User user)
        {
            var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
            var jwtIssuer = _config["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer missing");
            var jwtAudience = _config["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience missing");

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? "")
            };

            // If your User has BranchId / RoleId, add them as claims here:
            // claims.Add(new Claim("BranchId", user.BranchId.ToString()));
            // claims.Add(new Claim(ClaimTypes.Role, "Admin"));

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

        private void SetRefreshCookie(string rawRefresh)
        {
            // Secure-by-default cookie; for cross-site SPA you may need SameSite=None and Secure=true.
            Response.Cookies.Append("refresh_token", rawRefresh, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/auth",
                Expires = DateTimeOffset.UtcNow.Add(RefreshTokenTtl)
            });
        }

    }
}

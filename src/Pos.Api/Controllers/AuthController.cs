using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Features.Auth.Commands.AssignRoleCommand;
using Pos.Application.Features.Auth.Commands.ForgotPassword;
using Pos.Application.Features.Auth.Commands.Login;
using Pos.Application.Features.Auth.Commands.Logout;
using Pos.Application.Features.Auth.Commands.RefreshToken;
using Pos.Application.Features.Auth.Commands.Register;
using Pos.Application.Features.Auth.Commands.ResetPassword;
using Pos.Application.Features.Auth.Dtos;


namespace Pos.Api.Controllers;


[ApiController]
[Asp.Versioning.ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/[controller]")]

public class AuthController(IMediator mediator, IWebHostEnvironment env) : ControllerBase
{
    private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(30);

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req, CancellationToken ct)
    {
        var res = await mediator.Send(new RegisterCommand(req), ct);
        SetRefreshCookie(res.RefreshToken!);
        res.RefreshToken = null; // don’t expose to client JS
        return Ok(res);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req, CancellationToken ct)
    {
        var res = await mediator.Send(new LoginCommand(req), ct);
        SetRefreshCookie(res.RefreshToken!);
        res.RefreshToken = null;
        return Ok(res);
    }


    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<RefreshResponse>> Refresh(CancellationToken ct)
    {
        var raw = Request.Cookies["refresh_token"];
        if (string.IsNullOrWhiteSpace(raw)) return Unauthorized();

        var res = await mediator.Send(new RefreshTokenCommand(raw), ct);
        SetRefreshCookie(res.RefreshToken!);
        res.RefreshToken = null;
        return Ok(res);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var raw = Request.Cookies["refresh_token"];
        await mediator.Send(new LogoutCommand(raw), ct);

        Response.Cookies.Delete("refresh_token", new CookieOptions
        {
            Path = "/"
        });

        return Ok(new { ok = true });
    }

    private void SetRefreshCookie(string rawRefresh)
    {
        Response.Cookies.Append("refresh_token", rawRefresh, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api",
            Expires = DateTimeOffset.UtcNow.Add(RefreshTokenTtl)
        });
    }

    [HttpPost("assign-role")]
    [Authorize(Policy = "ManageUsers")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest req, CancellationToken ct)
    {
        var ok = await mediator.Send(new AssignRoleCommand(req.UserId, req.RoleId), ct);
        return Ok(new { ok });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ForgotPasswordResult>> ForgotPassword([FromBody] ForgotPasswordRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new ForgotPasswordCommand(req.Email), ct);

        // ✅ PROD: never expose token
        if (!env.IsDevelopment())
            return Ok(new ForgotPasswordResult(true, null));

        // ✅ DEV: return token so you can test in Postman
        return Ok(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req, CancellationToken ct)
    {
        await mediator.Send(new ResetPasswordCommand(req.Token, req.NewPassword), ct);
        return Ok(new { ok = true });
    }
}
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiniCRM.Infrastructure;

namespace MiniCRM.API.Controllers;

public sealed record RegisterRequest([Required, EmailAddress, MaxLength(320)] string Email, [Required, MinLength(12)] string Password, [Required, MaxLength(80)] string FirstName, [Required, MaxLength(80)] string LastName);
public sealed record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public sealed record AuthUserDto(int Id, string Email, string FirstName, string LastName, IReadOnlyList<string> Roles);
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, AuthUserDto User);

[ApiController]
[Route("api/auth")]
public sealed class AuthController(UserManager<CrmUser> users, SignInManager<CrmUser> signIn, TokenService tokens, IWebHostEnvironment environment) : ControllerBase
{
    private const string RefreshCookie = "minicrm.refresh";

    [HttpPost("register"), AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var user = new CrmUser { UserName = request.Email.Trim(), Email = request.Email.Trim(), FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), CreatedAt = DateTime.UtcNow };
        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded) return BadRequest(new { errors = created.Errors.Select(x => new { x.Code, x.Description }) });
        var assigned = await users.AddToRoleAsync(user, "SalesRepresentative");
        if (!assigned.Succeeded) { await users.DeleteAsync(user); return Problem("The default user role is unavailable.", statusCode: 503); }
        return Ok(await IssueResponse(user, ct));
    }

    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive) return Unauthorized(new { error = "Email or password is incorrect." });
        var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded) return Unauthorized(new { error = "Email or password is incorrect." });
        return Ok(await IssueResponse(user, ct));
    }

    [HttpPost("refresh"), AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookie, out var raw) || string.IsNullOrWhiteSpace(raw)) return Unauthorized();
        var rotated = await tokens.RotateRefreshTokenAsync(raw, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        if (rotated is null) { Response.Cookies.Delete(RefreshCookie, CookieSettings()); return Unauthorized(); }
        SetRefreshCookie(rotated.Token, rotated.ExpiresAt);
        return Ok(await AccessResponse(rotated.User));
    }

    [HttpPost("logout"), AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        Request.Cookies.TryGetValue(RefreshCookie, out var raw); await tokens.RevokeRefreshTokenAsync(raw, ct);
        if(int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var userId)) await tokens.RevokeAccessTokenAsync(User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti),userId,ct);
        Response.Cookies.Delete(RefreshCookie, CookieSettings()); return NoContent();
    }

    [HttpGet("me"), Authorize]
    public async Task<IActionResult> Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier); var user = id is null ? null : await users.FindByIdAsync(id);
        return user is null || !user.IsActive ? Unauthorized() : Ok(await ToAuthUser(user));
    }

    [HttpPost("change-password"), Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier); var user = id is null ? null : await users.FindByIdAsync(id);
        if (user is null) return Unauthorized();
        var result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if(result.Succeeded) await users.UpdateSecurityStampAsync(user);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors.Select(x => new { x.Code, x.Description }) });
    }

    private async Task<AuthResponse> IssueResponse(CrmUser user, CancellationToken ct)
    {
        var refresh = await tokens.IssueRefreshTokenAsync(user, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        SetRefreshCookie(refresh.Token, refresh.ExpiresAt); return await AccessResponse(user);
    }
    private async Task<AuthResponse> AccessResponse(CrmUser user) { var access = await tokens.CreateAccessTokenAsync(user, HttpContext.RequestAborted); return new(access.Token, access.ExpiresAt, await ToAuthUser(user)); }
    private async Task<AuthUserDto> ToAuthUser(CrmUser user) => new(user.Id, user.Email ?? "", user.FirstName, user.LastName, (await users.GetRolesAsync(user)).ToArray());
    private void SetRefreshCookie(string raw, DateTime expires) => Response.Cookies.Append(RefreshCookie, raw, CookieSettings(expires));
    private CookieOptions CookieSettings(DateTime? expires = null) => new() { HttpOnly = true, Secure = Request.IsHttps || !environment.IsDevelopment(), SameSite = SameSiteMode.Lax, IsEssential = true, Path = "/api/auth", Expires = expires };
}

public sealed record ChangePasswordRequest([Required] string CurrentPassword, [Required, MinLength(12)] string NewPassword);

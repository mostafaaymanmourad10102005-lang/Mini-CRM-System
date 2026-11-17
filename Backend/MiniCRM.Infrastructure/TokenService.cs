using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MiniCRM.Infrastructure;

public sealed record RefreshTokenIssue(string Token, DateTime ExpiresAt);
public sealed record RotatedRefreshToken(CrmUser User, string Token, DateTime ExpiresAt);

public sealed class TokenService
{
    private readonly CrmDbContext _db;
    private readonly byte[] _key;
    private readonly IConfiguration _configuration;
    private readonly UserManager<CrmUser> _users;
    public TokenService(CrmDbContext db, IConfiguration configuration, UserManager<CrmUser> users)
    {
        _db = db; _configuration = configuration; _users = users;
        var signingKey = configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey must be provided via user-secrets or environment configuration.");
        _key = Encoding.UTF8.GetBytes(signingKey);
        if (_key.Length < 32) throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 UTF-8 bytes.");
    }

    public async Task<(string Token, DateTime ExpiresAt)> CreateAccessTokenAsync(CrmUser user, CancellationToken ct)
    {
        var now = DateTime.UtcNow; var expires = now.AddMinutes(15); var issuer = _configuration["Jwt:Issuer"] ?? "MiniCRM"; var audience = _configuration["Jwt:Audience"] ?? "MiniCRM.React";
        var stamp = await _users.GetSecurityStampAsync(user);
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new(JwtRegisteredClaimNames.Email, user.Email ?? ""), new(ClaimTypes.Name, user.UserName ?? user.Email ?? ""), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")), new("minicrm:security_stamp", stamp ?? "") };
        foreach (var role in await _users.GetRolesAsync(user)) claims.Add(new Claim(ClaimTypes.Role, role));
        var credentials = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, now, expires, credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public async Task<RefreshTokenIssue> IssueRefreshTokenAsync(CrmUser user, string? ipAddress, CancellationToken ct)
    {
        var raw = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64)); var expires = DateTime.UtcNow.AddDays(14);
        _db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = Hash(raw), ExpiresAt = expires, CreatedAt = DateTime.UtcNow, CreatedByIp = ipAddress });
        await _db.SaveChangesAsync(ct); return new(raw, expires);
    }

    public async Task<RotatedRefreshToken?> RotateRefreshTokenAsync(string raw, string? ipAddress, CancellationToken ct)
    {
        var hash = Hash(raw);
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var stored = await _db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (stored is null) return null;
        if (stored.RevokedAt is not null)
        {
            await _db.RefreshTokens.Where(x => x.UserId == stored.UserId && x.RevokedAt == null).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedAt, DateTime.UtcNow), ct);
            await transaction.CommitAsync(ct); return null;
        }
        if (stored.ExpiresAt <= DateTime.UtcNow) { stored.RevokedAt = DateTime.UtcNow; await _db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return null; }
        var user = await _users.FindByIdAsync(stored.UserId.ToString());
        if (user is null || !user.IsActive) { stored.RevokedAt = DateTime.UtcNow; await _db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return null; }
        var nextRaw = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64)); var nextHash = Hash(nextRaw); var expires = DateTime.UtcNow.AddDays(14);
        stored.RevokedAt = DateTime.UtcNow; stored.ReplacedByTokenHash = nextHash;
        _db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = nextHash, ExpiresAt = expires, CreatedAt = DateTime.UtcNow, CreatedByIp = ipAddress });
        await _db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return new(user, nextRaw, expires);
    }

    public async Task RevokeRefreshTokenAsync(string? raw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;
        var token = await _db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == Hash(raw), ct);
        if (token is null || token.RevokedAt is not null) return;
        token.RevokedAt = DateTime.UtcNow; await _db.SaveChangesAsync(ct);
    }

    public async Task RevokeAccessTokenAsync(string? tokenId,int userId,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(tokenId))return;
        var now=DateTime.UtcNow;
        await _db.RevokedAccessTokens.Where(x=>x.ExpiresAt<=now).ExecuteDeleteAsync(ct);
        if(!await _db.RevokedAccessTokens.AnyAsync(x=>x.TokenId==tokenId,ct))
            _db.RevokedAccessTokens.Add(new RevokedAccessToken{TokenId=tokenId,UserId=userId,RevokedAt=now,ExpiresAt=now.AddMinutes(15)});
        await _db.SaveChangesAsync(ct);
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

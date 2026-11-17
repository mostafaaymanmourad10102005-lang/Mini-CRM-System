namespace MiniCRM.Infrastructure;

public sealed class RefreshToken
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? CreatedByIp { get; set; }
}

public sealed class RevokedAccessToken
{
    public string TokenId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniCRM.Domain;

namespace MiniCRM.Infrastructure;

public sealed class CrmDbContext(DbContextOptions<CrmDbContext> options)
    : IdentityDbContext<CrmUser, IdentityRole<int>, int>(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<CrmTask> CrmTasks => Set<CrmTask>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<RevokedAccessToken> RevokedAccessTokens => Set<RevokedAccessToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(CrmDbContext).Assembly);
        builder.Entity<CrmUser>(e =>
        {
            e.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(80).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.IsActive);
        });
        builder.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshTokens"); e.HasKey(x => x.Id); e.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.CreatedByIp).HasMaxLength(64); e.Property(x => x.RevokedAt).IsConcurrencyToken(); e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.UserId, x.ExpiresAt, x.RevokedAt });
            e.HasOne<CrmUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<RevokedAccessToken>(e =>
        {
            e.ToTable("RevokedAccessTokens");e.HasKey(x=>x.TokenId);e.Property(x=>x.TokenId).HasMaxLength(36);
            e.HasIndex(x=>x.ExpiresAt);e.HasOne<CrmUser>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

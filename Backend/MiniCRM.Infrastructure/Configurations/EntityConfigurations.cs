using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniCRM.Domain;

namespace MiniCRM.Infrastructure.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> e)
    {
        e.ToTable("Customers", t => { t.HasCheckConstraint("CK_Customers_Email_NotBlank", "LEN(LTRIM(RTRIM([Email]))) > 0"); });
        e.HasKey(x => x.Id); e.Property(x => x.FirstName).HasMaxLength(80).IsRequired(); e.Property(x => x.LastName).HasMaxLength(80).IsRequired();
        e.Property(x => x.Email).HasMaxLength(320).IsRequired(); e.Property(x => x.PhoneNumber).HasMaxLength(32);
        e.Property(x => x.CompanyName).HasMaxLength(160); e.Property(x => x.JobTitle).HasMaxLength(120);
        e.Property(x => x.Address).HasMaxLength(300); e.Property(x => x.City).HasMaxLength(100); e.Property(x => x.Country).HasMaxLength(100);
        e.Property(x => x.Source).HasMaxLength(80); e.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()"); e.Property(x => x.UpdatedAt).IsConcurrencyToken();
        e.HasIndex(x => x.Email).IsUnique(); e.HasIndex(x => new { x.Status, x.CreatedAt }); e.HasIndex(x => x.AssignedToUserId);
        e.HasOne<CrmUser>().WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> e)
    {
        e.ToTable("Contacts"); e.HasKey(x => x.Id); e.Property(x => x.FullName).HasMaxLength(160).IsRequired();
        e.Property(x => x.Email).HasMaxLength(320); e.Property(x => x.PhoneNumber).HasMaxLength(32); e.Property(x => x.JobTitle).HasMaxLength(120);
        e.HasOne(x => x.Customer).WithMany(x => x.Contacts).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
        e.HasIndex(x => x.CustomerId); e.HasIndex(x => x.CustomerId).IsUnique().HasFilter("[IsPrimary] = 1");
    }
}

public sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> e)
    {
        e.ToTable("Leads", t => t.HasCheckConstraint("CK_Leads_EstimatedValue_Nonnegative", "[EstimatedValue] >= 0"));
        e.HasKey(x => x.Id); e.Property(x => x.Title).HasMaxLength(180).IsRequired(); e.Property(x => x.ContactName).HasMaxLength(160);
        e.Property(x => x.Email).HasMaxLength(320); e.Property(x => x.PhoneNumber).HasMaxLength(32); e.Property(x => x.Source).HasMaxLength(80);
        e.Property(x => x.Status).HasConversion<string>().HasMaxLength(24); e.Property(x => x.EstimatedValue).HasPrecision(18, 2);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()"); e.Property(x => x.UpdatedAt).IsConcurrencyToken();
        e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne<CrmUser>().WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.SetNull);
        e.HasIndex(x => new { x.Status, x.CreatedAt }); e.HasIndex(x => x.AssignedToUserId); e.HasIndex(x => x.Email);
    }
}

public sealed class OpportunityConfiguration : IEntityTypeConfiguration<Opportunity>
{
    public void Configure(EntityTypeBuilder<Opportunity> e)
    {
        e.ToTable("Opportunities", t => t.HasCheckConstraint("CK_Opportunities_Amount_Nonnegative", "[Amount] >= 0"));
        e.HasKey(x => x.Id); e.Property(x => x.Title).HasMaxLength(180).IsRequired(); e.Property(x => x.Description).HasMaxLength(2000);
        e.Property(x => x.Amount).HasPrecision(18, 2); e.Property(x => x.Stage).HasConversion<string>().HasMaxLength(32);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()"); e.Property(x => x.UpdatedAt).IsConcurrencyToken();
        e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne<Lead>().WithMany().HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne<CrmUser>().WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.SetNull);
        e.HasIndex(x => new { x.Stage, x.ExpectedCloseDate }); e.HasIndex(x => x.CustomerId); e.HasIndex(x => x.AssignedToUserId);
    }
}

public sealed class CrmTaskConfiguration : IEntityTypeConfiguration<CrmTask>
{
    public void Configure(EntityTypeBuilder<CrmTask> e)
    {
        e.ToTable("Tasks"); e.HasKey(x => x.Id); e.Property(x => x.Title).HasMaxLength(180).IsRequired(); e.Property(x => x.Description).HasMaxLength(2000);
        e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(16); e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne<Lead>().WithMany().HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne<Opportunity>().WithMany().HasForeignKey(x => x.OpportunityId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne<CrmUser>().WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.SetNull);
        e.HasIndex(x => new { x.Status, x.DueDate }); e.HasIndex(x => x.AssignedToUserId);
    }
}

public sealed class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> e)
    {
        e.ToTable("Notes"); e.HasKey(x => x.Id); e.Property(x => x.Content).HasMaxLength(8000).IsRequired(); e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne<Lead>().WithMany().HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne<Opportunity>().WithMany().HasForeignKey(x => x.OpportunityId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne<CrmUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => new { x.CustomerId, x.CreatedAt }); e.HasIndex(x => new { x.LeadId, x.CreatedAt }); e.HasIndex(x => new { x.OpportunityId, x.CreatedAt });
    }
}

public sealed class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> e)
    {
        e.ToTable("Activities"); e.HasKey(x => x.Id); e.Property(x => x.ActivityType).HasConversion<string>().HasMaxLength(16);
        e.Property(x => x.Description).HasMaxLength(4000).IsRequired(); e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne<Lead>().WithMany().HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne<Opportunity>().WithMany().HasForeignKey(x => x.OpportunityId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne<CrmUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => new { x.CustomerId, x.CreatedAt }); e.HasIndex(x => new { x.LeadId, x.CreatedAt }); e.HasIndex(x => new { x.OpportunityId, x.CreatedAt });
    }
}

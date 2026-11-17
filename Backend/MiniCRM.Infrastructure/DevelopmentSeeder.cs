using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniCRM.Domain;

namespace MiniCRM.Infrastructure;

public static class DevelopmentSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var users = services.GetRequiredService<UserManager<CrmUser>>();
        var db = services.GetRequiredService<CrmDbContext>();
        if (!await db.Users.AnyAsync(ct))
        {
            var admin = new CrmUser { UserName = "admin@minicrm.local", Email = "admin@minicrm.local", FirstName = "MiniCRM", LastName = "Admin", EmailConfirmed = true };
            var result = await users.CreateAsync(admin, Environment.GetEnvironmentVariable("MINICRM_SEED_ADMIN_PASSWORD") ?? throw new InvalidOperationException("Set MINICRM_SEED_ADMIN_PASSWORD when development seeding is enabled."));
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
            await users.AddToRoleAsync(admin, "Admin");
            var sales = new CrmUser { UserName = "sales@minicrm.local", Email = "sales@minicrm.local", FirstName = "Taylor", LastName = "Sales", EmailConfirmed = true };
            result = await users.CreateAsync(sales, Environment.GetEnvironmentVariable("MINICRM_SEED_SALES_PASSWORD") ?? throw new InvalidOperationException("Set MINICRM_SEED_SALES_PASSWORD when development seeding is enabled."));
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
            await users.AddToRoleAsync(sales, "SalesRepresentative");
            db.Customers.Add(new Customer { FirstName = "Avery", LastName = "Morgan", Email = "avery@example.test", CompanyName = "Northstar Labs", City = "Cairo", Country = "Egypt", Status = CustomerStatus.Active, AssignedToUserId = sales.Id });
            db.Leads.Add(new Lead { Title = "Regional expansion", ContactName = "Sam Rivera", Email = "sam@example.test", Source = "Referral", EstimatedValue = 18000, AssignedToUserId = sales.Id });
            await db.SaveChangesAsync(ct);
        }
    }
}

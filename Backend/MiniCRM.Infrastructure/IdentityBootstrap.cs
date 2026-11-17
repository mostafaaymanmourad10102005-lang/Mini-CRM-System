using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MiniCRM.Infrastructure;

public static class IdentityBootstrap
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, IHostEnvironment environment, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        if (configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup")) await db.Database.MigrateAsync(ct);
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        foreach (var roleName in new[] { "Admin", "Manager", "SalesRepresentative" })
            if (!await roles.RoleExistsAsync(roleName))
            {
                var result = await roles.CreateAsync(new IdentityRole<int>(roleName));
                if (!result.Succeeded) throw new InvalidOperationException($"Could not provision the {roleName} role: {string.Join("; ", result.Errors.Select(x => x.Description))}");
            }
        if (environment.IsDevelopment() && configuration.GetValue<bool>("Database:SeedDevelopmentData"))
            await DevelopmentSeeder.SeedAsync(scope.ServiceProvider, ct);
    }
}

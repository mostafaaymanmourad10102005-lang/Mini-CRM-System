using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MiniCRM.Infrastructure;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CrmDbContext>
{
    public CrmDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost;Database=MiniCRM;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True";
        var options = new DbContextOptionsBuilder<CrmDbContext>().UseSqlServer(connection).Options;
        return new CrmDbContext(options);
    }
}

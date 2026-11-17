using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MiniCRM.Infrastructure;

public sealed class SqlServerHealthCheck(CrmDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try { return await db.Database.CanConnectAsync(cancellationToken) ? HealthCheckResult.Healthy("SQL Server is reachable.") : HealthCheckResult.Unhealthy("SQL Server connection could not be established."); }
        catch (Exception ex) { return HealthCheckResult.Unhealthy("SQL Server connection could not be established.", ex); }
    }
}

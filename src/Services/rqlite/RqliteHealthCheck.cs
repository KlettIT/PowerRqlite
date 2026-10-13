using Microsoft.Extensions.Diagnostics.HealthChecks;
using PowerRqlite.Interfaces.rqlite;

namespace PowerRqlite.Services.rqlite
{
    /// <summary>
    /// GET /health: Healthy when rqlite answers a query, so a load balancer or orchestrator only routes PowerDNS
    /// to instances that can reach the cluster.
    /// </summary>
    public class RqliteHealthCheck(IRqliteService rqliteService) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await rqliteService.QueryAsync("SELECT 1");
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("rqlite is not reachable", ex);
            }
        }
    }
}

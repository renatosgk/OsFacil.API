using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OsFacil.HealthChecks
{
    public class ApiHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
           HealthCheckContext context,
           CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                HealthCheckResult.Healthy("API está funcionando normalmente")
            );
        }
    }
}

using global::MongoDB.Bson;
using global::MongoDB.Driver;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OsFacil.MongoDB;

namespace OsFacil.HealthChecks;

public class MongoDbHealthCheck : IHealthCheck
{
    private readonly MongoDbSettings _settings;

    public MongoDbHealthCheck(IOptions<MongoDbSettings> settings) => _settings = settings.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = new MongoClient(_settings.ConnectionString);
            var db = client.GetDatabase(_settings.DatabaseName);
            await db.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("MongoDB respondendo normalmente.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded($"MongoDB indisponível: {ex.Message}");
        }
    }
}

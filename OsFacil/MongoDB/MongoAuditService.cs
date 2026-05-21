using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace OsFacil.MongoDB;

public class MongoAuditService : IMongoAuditService
{
    private readonly IMongoCollection<AuditLog> _collection;
    private readonly ILogger<MongoAuditService> _logger;

    public MongoAuditService(IOptions<MongoDbSettings> settings, ILogger<MongoAuditService> logger)
    {
        _logger = logger;
        try
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var db = client.GetDatabase(settings.Value.DatabaseName);
            _collection = db.GetCollection<AuditLog>(settings.Value.AuditLogsCollection);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MongoDB não disponível. Auditoria desativada.");
            _collection = null!;
        }
    }

    public async Task RegistrarAsync(string entidade, string operacao, long? entidadeId = null,
        string? usuarioEmail = null, string detalhes = "")
    {
        if (_collection == null) return;
        try
        {
            await _collection.InsertOneAsync(new AuditLog
            {
                Entidade = entidade,
                Operacao = operacao,
                EntidadeId = entidadeId,
                UsuarioEmail = usuarioEmail,
                Detalhes = detalhes,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao registrar auditoria no MongoDB.");
        }
    }

    public async Task<List<AuditLog>> ObterLogsAsync(string? entidade = null, int limite = 100)
    {
        if (_collection == null) return new List<AuditLog>();
        try
        {
            var filtro = entidade != null
                ? Builders<AuditLog>.Filter.Eq(x => x.Entidade, entidade)
                : Builders<AuditLog>.Filter.Empty;
            return await _collection.Find(filtro)
                .SortByDescending(x => x.Timestamp)
                .Limit(limite)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao consultar auditoria no MongoDB.");
            return new List<AuditLog>();
        }
    }
}

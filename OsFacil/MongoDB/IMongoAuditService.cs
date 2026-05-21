namespace OsFacil.MongoDB;

public interface IMongoAuditService
{
    Task RegistrarAsync(string entidade, string operacao, long? entidadeId = null,
        string? usuarioEmail = null, string detalhes = "");
    Task<List<AuditLog>> ObterLogsAsync(string? entidade = null, int limite = 100);
}

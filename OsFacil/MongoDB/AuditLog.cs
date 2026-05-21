using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OsFacil.MongoDB;

public class AuditLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Entidade { get; set; } = string.Empty;
    public string Operacao { get; set; } = string.Empty;
    public long? EntidadeId { get; set; }
    public string? UsuarioEmail { get; set; }
    public string Detalhes { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

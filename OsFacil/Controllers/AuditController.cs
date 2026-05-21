using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OsFacil.MongoDB;

namespace OsFacil.Controllers;

/// <summary>Consulta de logs de auditoria (MongoDB)</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class AuditController : ControllerBase
{
    private readonly IMongoAuditService _audit;

    public AuditController(IMongoAuditService audit) => _audit = audit;

    /// <summary>Lista logs de auditoria armazenados no MongoDB</summary>
    /// <param name="entidade">Filtrar por entidade (ex: Usuario, Carro, OrdemServico)</param>
    /// <param name="limite">Quantidade máxima de registros (padrão: 100)</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AuditLog>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs([FromQuery] string? entidade, [FromQuery] int limite = 100)
    {
        var logs = await _audit.ObterLogsAsync(entidade, limite);
        return Ok(logs);
    }
}

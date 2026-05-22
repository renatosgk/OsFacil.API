using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OsFacil.MongoDB;

namespace OsFacil.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class AuditController : ControllerBase
{
    private readonly IMongoAuditService _audit;

    public AuditController(IMongoAuditService audit) => _audit = audit;

 
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AuditLog>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs([FromQuery] string? entidade, [FromQuery] int limite = 100)
    {
        var logs = await _audit.ObterLogsAsync(entidade, limite);
        return Ok(logs);
    }
}

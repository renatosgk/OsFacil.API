using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OsFacil.Common;
using OsFacil.Data;
using OsFacil.DTO.Request;
using OsFacil.DTO.Response;
using OsFacil.Messaging;
using OsFacil.Models;
using OsFacil.MongoDB;

namespace OsFacil.Controllers;


[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class FuncionariosController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly IMapper _mapper;
    private readonly ILogger<FuncionariosController> _log;
    private readonly RabbitMqProducer _bus;
    private readonly IMongoAuditService _audit;

    public FuncionariosController(AppDbContext ctx, IMapper mapper, ILogger<FuncionariosController> log,
        RabbitMqProducer bus, IMongoAuditService audit)
    {
        _ctx = ctx;
        _mapper = mapper;
        _log = log;
        _bus = bus;
        _audit = audit;
    }

    
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<HateoasResponse<FuncionarioResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams p)
    {
        var query = _ctx.Funcionarios.AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Filter))
            query = query.Where(f => f.Nome.Contains(p.Filter) || f.Cargo.Contains(p.Filter));

        query = (p.OrderBy?.ToLower(), p.OrderDir.ToLower()) switch
        {
            ("nome", "desc") => query.OrderByDescending(f => f.Nome),
            ("nome", _) => query.OrderBy(f => f.Nome),
            ("cargo", "desc") => query.OrderByDescending(f => f.Cargo),
            ("cargo", _) => query.OrderBy(f => f.Cargo),
            ("salario", "desc") => query.OrderByDescending(f => f.Salario),
            ("salario", _) => query.OrderBy(f => f.Salario),
            _ => query.OrderBy(f => f.Id)
        };

        var total = await query.CountAsync();
        var items = await query.Skip((p.Page - 1) * p.PageSize).Take(p.PageSize).ToListAsync();

        var mapped = _mapper.Map<IEnumerable<FuncionarioResponse>>(items)
            .Select(f => new HateoasResponse<FuncionarioResponse>(f)
                .AddLink(Url?.Action(nameof(GetById), new { id = f.Id }) ?? string.Empty, "self")
                .AddLink(Url?.Action(nameof(Update), new { id = f.Id }) ?? string.Empty, "update", "PUT")
                .AddLink(Url?.Action(nameof(Delete), new { id = f.Id }) ?? string.Empty, "delete", "DELETE"))
            .ToList();

        return Ok(new PagedResult<HateoasResponse<FuncionarioResponse>>
        {
            Data = mapped, Page = p.Page, PageSize = p.PageSize, TotalCount = total
        });
    }

    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(HateoasResponse<FuncionarioResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        var funcionario = await _ctx.Funcionarios.FindAsync(id);
        if (funcionario == null) return NotFound();

        var response = new HateoasResponse<FuncionarioResponse>(_mapper.Map<FuncionarioResponse>(funcionario))
            .AddLink(Url?.Action(nameof(GetById), new { id }) ?? string.Empty, "self")
            .AddLink(Url?.Action(nameof(Update), new { id }) ?? string.Empty, "update", "PUT")
            .AddLink(Url?.Action(nameof(Delete), new { id }) ?? string.Empty, "delete", "DELETE")
            .AddLink(Url?.Action(nameof(GetAll)) ?? string.Empty, "collection");

        return Ok(response);
    }

    
    [HttpPost]
    [ProducesResponseType(typeof(FuncionarioResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(FuncionarioRequest request)
    {
        var funcionario = _mapper.Map<Funcionario>(request);
        _ctx.Funcionarios.Add(funcionario);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Funcionário {Nome} cadastrado.", funcionario.Nome);
        _bus.SendMessage($"FUNCIONARIO_CRIADO|Id:{funcionario.Id}|Nome:{funcionario.Nome}|Cargo:{funcionario.Cargo}");
        await _audit.RegistrarAsync("Funcionario", "CRIACAO", funcionario.Id, detalhes: $"Cargo: {funcionario.Cargo}");

        return CreatedAtAction(nameof(GetById), new { id = funcionario.Id },
            _mapper.Map<FuncionarioResponse>(funcionario));
    }

    
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, FuncionarioRequest request)
    {
        var existing = await _ctx.Funcionarios.FindAsync(id);
        if (existing == null) return NotFound();

        _mapper.Map(request, existing);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Funcionário {Id} atualizado.", id);
        _bus.SendMessage($"FUNCIONARIO_ATUALIZADO|Id:{id}|Nome:{existing.Nome}");
        await _audit.RegistrarAsync("Funcionario", "ATUALIZACAO", id);

        return NoContent();
    }

    
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(long id)
    {
        var funcionario = await _ctx.Funcionarios.FindAsync(id);
        if (funcionario == null) return NotFound();

        try
        {
            _ctx.Funcionarios.Remove(funcionario);
            await _ctx.SaveChangesAsync();
            _log.LogInformation("Funcionário {Id} removido.", id);
            _bus.SendMessage($"FUNCIONARIO_REMOVIDO|Id:{id}");
            await _audit.RegistrarAsync("Funcionario", "REMOCAO", id);
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            _log.LogError(ex, "Erro ao remover funcionário {Id}.", id);
            return BadRequest("Não é possível remover um funcionário com Ordens de Serviço vinculadas.");
        }
    }
}

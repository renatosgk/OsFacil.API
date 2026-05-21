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

/// <summary>Gerenciamento de veículos</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class CarrosController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly IMapper _mapper;
    private readonly ILogger<CarrosController> _log;
    private readonly RabbitMqProducer _bus;
    private readonly IMongoAuditService _audit;

    public CarrosController(AppDbContext ctx, IMapper mapper, ILogger<CarrosController> log,
        RabbitMqProducer bus, IMongoAuditService audit)
    {
        _ctx = ctx;
        _mapper = mapper;
        _log = log;
        _bus = bus;
        _audit = audit;
    }

    /// <summary>Lista veículos com paginação e filtro</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<HateoasResponse<CarroResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams p)
    {
        var query = _ctx.Carros.Include(c => c.Usuario).AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Filter))
            query = query.Where(c => c.Placa.Contains(p.Filter) || c.Modelo.Contains(p.Filter) || c.Marca.Contains(p.Filter));

        query = (p.OrderBy?.ToLower(), p.OrderDir.ToLower()) switch
        {
            ("placa", "desc") => query.OrderByDescending(c => c.Placa),
            ("placa", _) => query.OrderBy(c => c.Placa),
            ("marca", "desc") => query.OrderByDescending(c => c.Marca),
            ("marca", _) => query.OrderBy(c => c.Marca),
            ("ano", "desc") => query.OrderByDescending(c => c.Ano),
            ("ano", _) => query.OrderBy(c => c.Ano),
            _ => query.OrderBy(c => c.Id)
        };

        var total = await query.CountAsync();
        var items = await query.Skip((p.Page - 1) * p.PageSize).Take(p.PageSize).ToListAsync();

        var mapped = _mapper.Map<IEnumerable<CarroResponse>>(items)
            .Select(c => new HateoasResponse<CarroResponse>(c)
                .AddLink(Url?.Action(nameof(GetById), new { id = c.Id }) ?? string.Empty, "self")
                .AddLink(Url?.Action(nameof(Update), new { id = c.Id }) ?? string.Empty, "update", "PUT")
                .AddLink(Url?.Action(nameof(Delete), new { id = c.Id }) ?? string.Empty, "delete", "DELETE"))
            .ToList();

        return Ok(new PagedResult<HateoasResponse<CarroResponse>>
        {
            Data = mapped, Page = p.Page, PageSize = p.PageSize, TotalCount = total
        });
    }

    /// <summary>Obtém veículo por ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(HateoasResponse<CarroResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        var carro = await _ctx.Carros.Include(c => c.Usuario).FirstOrDefaultAsync(c => c.Id == id);
        if (carro == null) return NotFound();

        var response = new HateoasResponse<CarroResponse>(_mapper.Map<CarroResponse>(carro))
            .AddLink(Url?.Action(nameof(GetById), new { id }) ?? string.Empty, "self")
            .AddLink(Url?.Action(nameof(Update), new { id }) ?? string.Empty, "update", "PUT")
            .AddLink(Url?.Action(nameof(Delete), new { id }) ?? string.Empty, "delete", "DELETE")
            .AddLink(Url?.Action(nameof(GetAll)) ?? string.Empty, "collection");

        return Ok(response);
    }

    /// <summary>Cadastra novo veículo</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CarroResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CarroRequest request)
    {
        var usuario = await _ctx.Usuarios.FindAsync(request.UsuarioId);
        if (usuario == null)
        {
            _log.LogWarning("UsuarioId inexistente: {Id}", request.UsuarioId);
            return BadRequest($"O usuário com ID {request.UsuarioId} não existe.");
        }

        var carro = _mapper.Map<Carro>(request);
        _ctx.Carros.Add(carro);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Carro {Placa} cadastrado.", carro.Placa);
        _bus.SendMessage($"CARRO_CADASTRADO|Id:{carro.Id}|Placa:{carro.Placa}|Dono:{usuario.Nome}");
        await _audit.RegistrarAsync("Carro", "CRIACAO", carro.Id, usuario.Email, $"Placa: {carro.Placa}");

        return CreatedAtAction(nameof(GetById), new { id = carro.Id }, _mapper.Map<CarroResponse>(carro));
    }

    /// <summary>Atualiza veículo</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, CarroRequest request)
    {
        var existing = await _ctx.Carros.FindAsync(id);
        if (existing == null) return NotFound();

        _mapper.Map(request, existing);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Carro {Id} atualizado.", id);
        _bus.SendMessage($"CARRO_ATUALIZADO|Id:{id}|Placa:{existing.Placa}");
        await _audit.RegistrarAsync("Carro", "ATUALIZACAO", id);

        return NoContent();
    }

    /// <summary>Remove veículo</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(long id)
    {
        var carro = await _ctx.Carros.FindAsync(id);
        if (carro == null) return NotFound();

        try
        {
            _ctx.Carros.Remove(carro);
            await _ctx.SaveChangesAsync();
            _log.LogInformation("Carro {Id} removido.", id);
            _bus.SendMessage($"CARRO_REMOVIDO|Id:{id}|Placa:{carro.Placa}");
            await _audit.RegistrarAsync("Carro", "REMOCAO", id);
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            _log.LogError(ex, "Erro ao excluir carro {Id}.", id);
            return BadRequest("Não é possível excluir um carro com Ordens de Serviço vinculadas.");
        }
    }
}

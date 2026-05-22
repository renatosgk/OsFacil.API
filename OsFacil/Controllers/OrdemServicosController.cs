using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OsFacil.Common;
using OsFacil.Data;
using OsFacil.DTO.Request;
using OsFacil.DTO.Response;
using OsFacil.Enum;
using OsFacil.Messaging;
using OsFacil.Models;
using OsFacil.MongoDB;

namespace OsFacil.Controllers;


[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class OrdemServicoController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly IMapper _mapper;
    private readonly ILogger<OrdemServicoController> _log;
    private readonly RabbitMqProducer _bus;
    private readonly IMongoAuditService _audit;

    public OrdemServicoController(AppDbContext ctx, IMapper mapper, ILogger<OrdemServicoController> log,
        RabbitMqProducer bus, IMongoAuditService audit)
    {
        _ctx = ctx;
        _mapper = mapper;
        _log = log;
        _bus = bus;
        _audit = audit;
    }

    
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<HateoasResponse<OrdemServicoResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams p)
    {
        _log.LogInformation("Listando OSs - Página {Page}", p.Page);

        var query = _ctx.OrdensServico
            .Include(o => o.Usuario)
            .Include(o => o.Carro)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Filter))
            query = query.Where(o => o.Descricao.Contains(p.Filter));

        query = (p.OrderBy?.ToLower(), p.OrderDir.ToLower()) switch
        {
            ("data", "desc") => query.OrderByDescending(o => o.DataCriacao),
            ("data", _) => query.OrderBy(o => o.DataCriacao),
            ("valor", "desc") => query.OrderByDescending(o => o.Valor),
            ("valor", _) => query.OrderBy(o => o.Valor),
            ("status", "desc") => query.OrderByDescending(o => o.Status),
            ("status", _) => query.OrderBy(o => o.Status),
            _ => query.OrderByDescending(o => o.DataCriacao)
        };

        var total = await query.CountAsync();
        var items = await query.Skip((p.Page - 1) * p.PageSize).Take(p.PageSize).ToListAsync();

        var mapped = _mapper.Map<IEnumerable<OrdemServicoResponse>>(items)
            .Select(o => new HateoasResponse<OrdemServicoResponse>(o)
                .AddLink(Url?.Action(nameof(GetById), new { id = o.Id }) ?? string.Empty, "self")
                .AddLink(Url?.Action(nameof(Update), new { id = o.Id }) ?? string.Empty, "update", "PUT")
                .AddLink(Url?.Action(nameof(UpdateStatus), new { id = o.Id }) ?? string.Empty, "update-status", "PATCH")
                .AddLink(Url?.Action(nameof(Delete), new { id = o.Id }) ?? string.Empty, "delete", "DELETE"))
            .ToList();

        return Ok(new PagedResult<HateoasResponse<OrdemServicoResponse>>
        {
            Data = mapped, Page = p.Page, PageSize = p.PageSize, TotalCount = total
        });
    }

   
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(HateoasResponse<OrdemServicoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        var os = await _ctx.OrdensServico
            .Include(o => o.Usuario)
            .Include(o => o.Carro)
            .Include(o => o.Funcionario)
            .Include(o => o.ItensServico)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (os == null)
        {
            _log.LogWarning("OS {Id} não encontrada.", id);
            return NotFound();
        }

        var response = new HateoasResponse<OrdemServicoResponse>(_mapper.Map<OrdemServicoResponse>(os))
            .AddLink(Url?.Action(nameof(GetById), new { id }) ?? string.Empty, "self")
            .AddLink(Url?.Action(nameof(Update), new { id }) ?? string.Empty, "update", "PUT")
            .AddLink(Url?.Action(nameof(UpdateStatus), new { id }) ?? string.Empty, "update-status", "PATCH")
            .AddLink(Url?.Action(nameof(Delete), new { id }) ?? string.Empty, "delete", "DELETE")
            .AddLink(Url?.Action(nameof(GetAll)) ?? string.Empty, "collection");

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrdemServicoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(OrdemServicoRequest request)
    {
        var user = await _ctx.Usuarios.FindAsync(request.UsuarioId);
        var car = await _ctx.Carros.FindAsync(request.CarroId);
        var func = await _ctx.Funcionarios.FindAsync(request.FuncionarioId);

        if (user == null || car == null || func == null)
        {
            _log.LogWarning("Falha ao criar OS: Referências inválidas.");
            return BadRequest("Verifique se UsuarioId, CarroId e FuncionarioId existem no banco.");
        }

        var os = _mapper.Map<OrdemServico>(request);
        _ctx.OrdensServico.Add(os);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Nova OS {Id} criada para {Placa}", os.Id, car.Placa);
        _bus.SendMessage($"OS_CRIADA|Id:{os.Id}|Carro:{car.Placa}|Cliente:{user.Nome}");
        await _audit.RegistrarAsync("OrdemServico", "CRIACAO", os.Id, user.Email,
            $"Carro: {car.Placa} | Funcionário: {func.Nome}");

        return CreatedAtAction(nameof(GetById), new { id = os.Id }, _mapper.Map<OrdemServicoResponse>(os));
    }

    
    [HttpPatch("{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] StatusOS novoStatus)
    {
        var os = await _ctx.OrdensServico.FindAsync(id);
        if (os == null) return NotFound();

        os.Status = novoStatus;
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Status da OS {Id} → {Status}", id, novoStatus);
        _bus.SendMessage($"OS_STATUS_ALTERADO|Id:{id}|NovoStatus:{novoStatus}");
        await _audit.RegistrarAsync("OrdemServico", "MUDANCA_STATUS", id, detalhes: $"Novo status: {novoStatus}");

        return NoContent();
    }

    
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(long id, OrdemServicoRequest request)
    {
        var existing = await _ctx.OrdensServico.FindAsync(id);
        if (existing == null) return NotFound();

        var func = await _ctx.Funcionarios.FindAsync(request.FuncionarioId);
        if (func == null) return BadRequest("Funcionário informado não existe.");

        _mapper.Map(request, existing);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("OS {Id} atualizada.", id);
        _bus.SendMessage($"OS_ATUALIZADA|Id:{id}");
        await _audit.RegistrarAsync("OrdemServico", "ATUALIZACAO", id);

        return NoContent();
    }

    
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        var os = await _ctx.OrdensServico.FindAsync(id);
        if (os == null) return NotFound();

        _ctx.OrdensServico.Remove(os);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("OS {Id} removida.", id);
        _bus.SendMessage($"OS_REMOVIDA|Id:{id}");
        await _audit.RegistrarAsync("OrdemServico", "REMOCAO", id);

        return NoContent();
    }
}

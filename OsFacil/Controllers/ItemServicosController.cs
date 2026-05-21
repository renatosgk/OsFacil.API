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

/// <summary>Itens de serviço vinculados a Ordens de Serviço</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ItemServicoController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly IMapper _mapper;
    private readonly ILogger<ItemServicoController> _log;
    private readonly RabbitMqProducer _bus;
    private readonly IMongoAuditService _audit;

    public ItemServicoController(AppDbContext ctx, IMapper mapper, ILogger<ItemServicoController> log,
        RabbitMqProducer bus, IMongoAuditService audit)
    {
        _ctx = ctx;
        _mapper = mapper;
        _log = log;
        _bus = bus;
        _audit = audit;
    }

    /// <summary>Lista todos os itens de serviço com paginação</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<HateoasResponse<ItemServicoResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams p)
    {
        var query = _ctx.ItensServico.AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Filter))
            query = query.Where(i => i.Descricao.Contains(p.Filter));

        query = (p.OrderBy?.ToLower(), p.OrderDir.ToLower()) switch
        {
            ("descricao", "desc") => query.OrderByDescending(i => i.Descricao),
            ("descricao", _) => query.OrderBy(i => i.Descricao),
            ("preco", "desc") => query.OrderByDescending(i => i.PrecoUnitario),
            ("preco", _) => query.OrderBy(i => i.PrecoUnitario),
            _ => query.OrderBy(i => i.Id)
        };

        var total = await query.CountAsync();
        var items = await query.Skip((p.Page - 1) * p.PageSize).Take(p.PageSize).ToListAsync();

        var mapped = _mapper.Map<IEnumerable<ItemServicoResponse>>(items)
            .Select(i => new HateoasResponse<ItemServicoResponse>(i)
                .AddLink(Url?.Action(nameof(GetById), new { id = i.Id }) ?? string.Empty, "self")
                .AddLink(Url?.Action(nameof(Update), new { id = i.Id }) ?? string.Empty, "update", "PUT")
                .AddLink(Url?.Action(nameof(Delete), new { id = i.Id }) ?? string.Empty, "delete", "DELETE"))
            .ToList();

        return Ok(new PagedResult<HateoasResponse<ItemServicoResponse>>
        {
            Data = mapped, Page = p.Page, PageSize = p.PageSize, TotalCount = total
        });
    }

    /// <summary>Lista itens de uma Ordem de Serviço específica</summary>
    /// <param name="ordemId">ID da Ordem de Serviço</param>
    [HttpGet("ordem/{ordemId}")]
    [ProducesResponseType(typeof(IEnumerable<HateoasResponse<ItemServicoResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOrdem(long ordemId)
    {
        var items = await _ctx.ItensServico.Where(i => i.OrdemServicoId == ordemId).ToListAsync();
        var mapped = _mapper.Map<IEnumerable<ItemServicoResponse>>(items)
            .Select(i => new HateoasResponse<ItemServicoResponse>(i)
                .AddLink(Url?.Action(nameof(GetById), new { id = i.Id }) ?? string.Empty, "self"))
            .ToList();
        return Ok(mapped);
    }

    /// <summary>Obtém item de serviço por ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(HateoasResponse<ItemServicoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        var item = await _ctx.ItensServico.FindAsync(id);
        if (item == null) return NotFound();

        var response = new HateoasResponse<ItemServicoResponse>(_mapper.Map<ItemServicoResponse>(item))
            .AddLink(Url?.Action(nameof(GetById), new { id }) ?? string.Empty, "self")
            .AddLink(Url?.Action(nameof(Update), new { id }) ?? string.Empty, "update", "PUT")
            .AddLink(Url?.Action(nameof(Delete), new { id }) ?? string.Empty, "delete", "DELETE")
            .AddLink(Url?.Action(nameof(GetAll)) ?? string.Empty, "collection");

        return Ok(response);
    }

    /// <summary>Adiciona item a uma Ordem de Serviço</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ItemServicoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(ItemServicoRequest request)
    {
        var os = await _ctx.OrdensServico.FindAsync(request.OrdemServicoId);
        if (os == null)
        {
            _log.LogWarning("OS {Id} inexistente.", request.OrdemServicoId);
            return BadRequest($"A Ordem de Serviço {request.OrdemServicoId} não existe.");
        }

        var item = _mapper.Map<ItemServico>(request);
        _ctx.ItensServico.Add(item);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Item '{Descricao}' adicionado à OS {OSId}.", item.Descricao, item.OrdemServicoId);
        _bus.SendMessage($"ITEM_ADICIONADO|OS:{item.OrdemServicoId}|Item:{item.Descricao}|Valor:{item.PrecoUnitario}");
        await _audit.RegistrarAsync("ItemServico", "CRIACAO", item.Id, detalhes: $"OS: {item.OrdemServicoId}");

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, _mapper.Map<ItemServicoResponse>(item));
    }

    /// <summary>Atualiza item de serviço</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, ItemServicoRequest request)
    {
        var existing = await _ctx.ItensServico.FindAsync(id);
        if (existing == null) return NotFound();

        _mapper.Map(request, existing);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Item {Id} atualizado.", id);
        _bus.SendMessage($"ITEM_ATUALIZADO|Id:{id}|OS:{existing.OrdemServicoId}");
        await _audit.RegistrarAsync("ItemServico", "ATUALIZACAO", id);

        return NoContent();
    }

    /// <summary>Remove item de serviço</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        var item = await _ctx.ItensServico.FindAsync(id);
        if (item == null) return NotFound();

        var ordemId = item.OrdemServicoId;
        _ctx.ItensServico.Remove(item);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Item {Id} removido.", id);
        _bus.SendMessage($"ITEM_REMOVIDO|Id:{id}|OS:{ordemId}");
        await _audit.RegistrarAsync("ItemServico", "REMOCAO", id);

        return NoContent();
    }
}

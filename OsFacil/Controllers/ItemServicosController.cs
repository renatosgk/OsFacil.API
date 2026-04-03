using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using OsFacil.Data;
using OsFacil.DTO.Request;
using OsFacil.DTO.Response;
using OsFacil.Messaging;
using OsFacil.Models;
using Microsoft.EntityFrameworkCore;

namespace OsFacil.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemServicoController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly IMapper _mapper;
    private readonly ILogger<ItemServicoController> _log;
    private readonly RabbitMqProducer _bus; 

    public ItemServicoController(AppDbContext ctx, IMapper mapper, ILogger<ItemServicoController> log, RabbitMqProducer bus)
    {
        _ctx = ctx;
        _mapper = mapper;
        _log = log;
        _bus = bus;
    }

    [HttpPost]
    public async Task<IActionResult> Create(ItemServicoRequest request)
    {
        var os = await _ctx.OrdensServico.FindAsync(request.OrdemServicoId);

        if (os == null)
        {
            _log.LogWarning("Falha ao adicionar item: Ordem de Serviço {OSId} inexistente.", request.OrdemServicoId);
            return BadRequest($"A Ordem de Serviço {request.OrdemServicoId} não existe.");
        }

        var item = _mapper.Map<ItemServico>(request);
        _ctx.ItensServico.Add(item);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Item '{Descricao}' adicionado à OS {OSId}.", item.Descricao, item.OrdemServicoId);

       
        _bus.SendMessage($"ITEM_ADICIONADO|OS:{item.OrdemServicoId}|Item:{item.Descricao}|Valor:{item.PrecoUnitario}");

        var response = _mapper.Map<ItemServicoResponse>(item);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, ItemServicoRequest request)
    {
        var existing = await _ctx.ItensServico.FindAsync(id);
        if (existing == null) return NotFound();

        _mapper.Map(request, existing);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Item de serviço {Id} atualizado.", id);

       
        _bus.SendMessage($"ITEM_ATUALIZADO|Id:{id}|OS:{existing.OrdemServicoId}");

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var item = await _ctx.ItensServico.FindAsync(id);
        if (item == null) return NotFound();

        var ordemId = item.OrdemServicoId; 

        _ctx.ItensServico.Remove(item);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Item de serviço {Id} removido.", id);

        
        _bus.SendMessage($"ITEM_REMOVIDO|Id:{id}|OS:{ordemId}");

        return NoContent();
    }

   
    [HttpGet("ordem/{ordemId}")]
    public async Task<IActionResult> GetByOrdem(long ordemId) =>
        Ok(_mapper.Map<IEnumerable<ItemServicoResponse>>(await _ctx.ItensServico.Where(i => i.OrdemServicoId == ordemId).ToListAsync()));

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(_mapper.Map<IEnumerable<ItemServicoResponse>>(await _ctx.ItensServico.ToListAsync()));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var item = await _ctx.ItensServico.FindAsync(id);
        return item == null ? NotFound() : Ok(_mapper.Map<ItemServicoResponse>(item));
    }
}
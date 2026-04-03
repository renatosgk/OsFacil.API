
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OsFacil.Data;
using OsFacil.DTO.Request;
using OsFacil.DTO.Response;
using OsFacil.Enum;
using OsFacil.Models;
using OsFacil.Messaging;

namespace OsFacil.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdemServicoController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly IMapper _mapper;
    private readonly ILogger<OrdemServicoController> _log;
    private readonly RabbitMqProducer _bus;

    public OrdemServicoController(AppDbContext ctx, IMapper mapper, ILogger<OrdemServicoController> log, RabbitMqProducer bus)
    {
        _ctx = ctx;
        _mapper = mapper;
        _log = log;
        _bus = bus;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        _log.LogInformation("Listando todas as Ordens de Serviço.");
        var ordens = await _ctx.OrdensServico
            .Include(o => o.Usuario)
            .Include(o => o.Carro)
            .ToListAsync();

       
        var response = _mapper.Map<IEnumerable<OrdemServicoResponse>>(ordens);
        return Ok(response);
    }

    [HttpGet("{id}")]
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

        return Ok(_mapper.Map<OrdemServicoResponse>(os));
    }

    [HttpPost]
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

        _log.LogInformation("Nova OS Criada: {Id} para o veículo {Placa}", os.Id, car.Placa);
        _bus.SendMessage($"OS_CRIADA|Id:{os.Id}|Carro:{car.Placa}|Cliente:{user.Nome}");

       
        var response = _mapper.Map<OrdemServicoResponse>(os);
        return CreatedAtAction(nameof(GetById), new { id = os.Id }, response);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] StatusOS novoStatus)
    {
        var os = await _ctx.OrdensServico.FindAsync(id);
        if (os == null) return NotFound();

        os.Status = novoStatus;
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Status da OS {Id} alterado para {Status}", id, novoStatus);
        _bus.SendMessage($"OS_STATUS_ALTERADO|Id:{id}|NovoStatus:{novoStatus}");
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, OrdemServicoRequest request)
    {
        var existing = await _ctx.OrdensServico.FindAsync(id);
        if (existing == null) return NotFound();

        
        var func = await _ctx.Funcionarios.FindAsync(request.FuncionarioId);
        if (func == null) return BadRequest("Funcionário informado não existe.");

        
        _mapper.Map(request, existing);

        await _ctx.SaveChangesAsync();
        _log.LogInformation("OS {Id} atualizada com sucesso.", id);
        _bus.SendMessage($"OS_ATUALIZADA|Id:{id}");
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var os = await _ctx.OrdensServico.FindAsync(id);
        if (os == null) return NotFound();

        _ctx.OrdensServico.Remove(os);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("OS {Id} removida.", id);
        _bus.SendMessage($"OS_REMOVIDA|Id:{id}");
        return NoContent();
    }
}
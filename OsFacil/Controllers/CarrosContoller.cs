using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OsFacil.Data;
using OsFacil.DTO.Request;
using OsFacil.DTO.Response;
using OsFacil.Models;
using OsFacil.Messaging;

namespace OsFacil.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarrosController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly IMapper _mapper;
    private readonly ILogger<CarrosController> _log;
    private readonly RabbitMqProducer _bus; 

    public CarrosController(AppDbContext ctx, IMapper mapper, ILogger<CarrosController> log, RabbitMqProducer bus)
    {
        _ctx = ctx;
        _mapper = mapper;
        _log = log;
        _bus = bus;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CarroRequest request)
    {
        var usuario = await _ctx.Usuarios.FindAsync(request.UsuarioId);
        if (usuario == null)
        {
            _log.LogWarning("Tentativa de cadastrar carro para UsuarioId inexistente: {User}", request.UsuarioId);
            return BadRequest($"O usuário com ID {request.UsuarioId} não existe.");
        }

        var carro = _mapper.Map<Carro>(request);
        _ctx.Carros.Add(carro);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Carro placa {Placa} cadastrado com sucesso.", carro.Placa);

        _bus.SendMessage($"CARRO_CADASTRADO|Id:{carro.Id}|Placa:{carro.Placa}|Dono:{usuario.Nome}");

        var response = _mapper.Map<CarroResponse>(carro);
        return CreatedAtAction(nameof(GetById), new { id = carro.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, CarroRequest request)
    {
        var existing = await _ctx.Carros.FindAsync(id);
        if (existing == null) return NotFound();

        _mapper.Map(request, existing);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Veículo ID {Id} (Placa: {Placa}) atualizado.", id, existing.Placa);

        _bus.SendMessage($"CARRO_ATUALIZADO|Id:{id}|Placa:{existing.Placa}");

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var carro = await _ctx.Carros.FindAsync(id);
        if (carro == null) return NotFound();

        try
        {
            _ctx.Carros.Remove(carro);
            await _ctx.SaveChangesAsync();

            _log.LogInformation("Carro ID {Id} (Placa: {Placa}) removido.", id, carro.Placa);

        
            _bus.SendMessage($"CARRO_REMOVIDO|Id:{id}|Placa:{carro.Placa}");

            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            _log.LogError(ex, "Erro de integridade ao excluir carro {Id}.", id);
            return BadRequest("Não é possível excluir um carro que possui histórico de Ordens de Serviço.");
        }
    }

 
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(_mapper.Map<IEnumerable<CarroResponse>>(await _ctx.Carros.Include(c => c.Usuario).ToListAsync()));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var carro = await _ctx.Carros.Include(c => c.Usuario).FirstOrDefaultAsync(c => c.Id == id);
        return carro == null ? NotFound() : Ok(_mapper.Map<CarroResponse>(carro));
    }
}
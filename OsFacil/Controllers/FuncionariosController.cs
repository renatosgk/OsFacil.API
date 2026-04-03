
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OsFacil.Data;
using OsFacil.DTO.Request;
using OsFacil.DTO.Response;
using OsFacil.Messaging;
using OsFacil.Models;

namespace OsFacil.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FuncionariosController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly IMapper _mapper;
    private readonly ILogger<FuncionariosController> _log;
    private readonly RabbitMqProducer _bus; 

    public FuncionariosController(AppDbContext ctx, IMapper mapper, ILogger<FuncionariosController> log, RabbitMqProducer bus)
    {
        _ctx = ctx;
        _mapper = mapper;
        _log = log;
        _bus = bus;
    }

    [HttpPost]
    public async Task<IActionResult> Create(FuncionarioRequest request)
    {
        var funcionario = _mapper.Map<Funcionario>(request);

        _ctx.Funcionarios.Add(funcionario);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Funcionário cadastrado: {Nome} - Cargo: {Cargo}", funcionario.Nome, funcionario.Cargo);

      
        _bus.SendMessage($"FUNCIONARIO_CRIADO|Id:{funcionario.Id}|Nome:{funcionario.Nome}|Cargo:{funcionario.Cargo}");

        var response = _mapper.Map<FuncionarioResponse>(funcionario);
        return CreatedAtAction(nameof(GetById), new { id = funcionario.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, FuncionarioRequest request)
    {
        var existing = await _ctx.Funcionarios.FindAsync(id);
        if (existing == null) return NotFound();

        _mapper.Map(request, existing);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Dados do funcionário ID {Id} atualizados.", id);

        
        _bus.SendMessage($"FUNCIONARIO_ATUALIZADO|Id:{id}|Nome:{existing.Nome}");

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var funcionario = await _ctx.Funcionarios.FindAsync(id);
        if (funcionario == null) return NotFound();

        try
        {
            _ctx.Funcionarios.Remove(funcionario);
            await _ctx.SaveChangesAsync();

            _log.LogInformation("Funcionário ID {Id} removido do sistema.", id);

           
            _bus.SendMessage($"FUNCIONARIO_REMOVIDO|Id:{id}");

            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            _log.LogError(ex, "Erro ao remover funcionário {Id}.", id);
            return BadRequest("Não é possível remover um funcionário que já possui Ordens de Serviço.");
        }
    }

 
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(_mapper.Map<IEnumerable<FuncionarioResponse>>(await _ctx.Funcionarios.ToListAsync()));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var funcionario = await _ctx.Funcionarios.FindAsync(id);
        return funcionario == null ? NotFound() : Ok(_mapper.Map<FuncionarioResponse>(funcionario));
    }
}
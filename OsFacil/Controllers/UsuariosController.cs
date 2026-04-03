
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
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly IMapper _mapper;
    private readonly ILogger<UsuariosController> _log;
    private readonly RabbitMqProducer _bus;

    public UsuariosController(AppDbContext ctx, IMapper mapper, ILogger<UsuariosController> log, RabbitMqProducer bus)
    {
        _ctx = ctx;
        _mapper = mapper;
        _log = log;
        _bus = bus;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        _log.LogInformation("Listando todos os usuários/clientes");

        var usuarios = await _ctx.Usuarios
            .Include(u => u.Carros)
            .ToListAsync();

        
        var response = _mapper.Map<IEnumerable<UsuarioResponse>>(usuarios);
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var usuario = await _ctx.Usuarios
            .Include(u => u.Carros)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario == null)
        {
            _log.LogWarning("Usuário {UsuarioId} não encontrado", id);
            return NotFound();
        }

        return Ok(_mapper.Map<UsuarioResponse>(usuario));
    }

    [HttpPost]
    public async Task<IActionResult> Create(UsuarioRequest request)
    {
        
        var usuario = _mapper.Map<Usuario>(request);

        
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        _ctx.Usuarios.Add(usuario);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Usuário criado: {Id} - {Email}", usuario.Id, usuario.Email);
        _bus.SendMessage($"USUARIO_CRIADO|Id:{usuario.Id}|Email:{usuario.Email}");

        var response = _mapper.Map<UsuarioResponse>(usuario);
        return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, UsuarioRequest request)
    {
        var existing = await _ctx.Usuarios.FindAsync(id);
        if (existing == null) return NotFound();

      
        _mapper.Map(request, existing);

        await _ctx.SaveChangesAsync();
        _log.LogInformation("Usuário {Id} atualizado.", id);
        _bus.SendMessage($"USUARIO_ATUALIZADO|Id:{id}");
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var usuario = await _ctx.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound();

        try
        {
            _ctx.Usuarios.Remove(usuario);
            await _ctx.SaveChangesAsync();
            _log.LogInformation("Usuário {Id} removido.", id);
            _bus.SendMessage($"USUARIO_REMOVIDO|Id:{id}");
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            _log.LogError(ex, "Erro ao remover usuário {Id}. Verifique se existem carros vinculados.", id);
            return BadRequest("Não é possível remover um usuário que possui carros cadastrados.");
        }
    }
}
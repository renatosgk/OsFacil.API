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

/// <summary>Gerenciamento de usuários/clientes</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly IMapper _mapper;
    private readonly ILogger<UsuariosController> _log;
    private readonly RabbitMqProducer _bus;
    private readonly IMongoAuditService _audit;

    public UsuariosController(AppDbContext ctx, IMapper mapper, ILogger<UsuariosController> log,
        RabbitMqProducer bus, IMongoAuditService audit)
    {
        _ctx = ctx;
        _mapper = mapper;
        _log = log;
        _bus = bus;
        _audit = audit;
    }

    /// <summary>Lista usuários com paginação e filtro</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<HateoasResponse<UsuarioResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams p)
    {
        _log.LogInformation("Listando usuários - Página {Page}, Tamanho {Size}", p.Page, p.PageSize);

        var query = _ctx.Usuarios.Include(u => u.Carros).AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Filter))
            query = query.Where(u => u.Nome.Contains(p.Filter) || u.Email.Contains(p.Filter));

        query = (p.OrderBy?.ToLower(), p.OrderDir.ToLower()) switch
        {
            ("nome", "desc") => query.OrderByDescending(u => u.Nome),
            ("nome", _) => query.OrderBy(u => u.Nome),
            ("email", "desc") => query.OrderByDescending(u => u.Email),
            ("email", _) => query.OrderBy(u => u.Email),
            ("criado", "desc") => query.OrderByDescending(u => u.CriadoEm),
            _ => query.OrderBy(u => u.Id)
        };

        var total = await query.CountAsync();
        var items = await query.Skip((p.Page - 1) * p.PageSize).Take(p.PageSize).ToListAsync();

        var mapped = _mapper.Map<IEnumerable<UsuarioResponse>>(items)
            .Select(u => new HateoasResponse<UsuarioResponse>(u)
                .AddLink(Url?.Action(nameof(GetById), new { id = u.Id }) ?? string.Empty, "self")
                .AddLink(Url?.Action(nameof(Update), new { id = u.Id }) ?? string.Empty, "update", "PUT")
                .AddLink(Url?.Action(nameof(Delete), new { id = u.Id }) ?? string.Empty, "delete", "DELETE"))
            .ToList();

        return Ok(new PagedResult<HateoasResponse<UsuarioResponse>>
        {
            Data = mapped,
            Page = p.Page,
            PageSize = p.PageSize,
            TotalCount = total
        });
    }

    /// <summary>Obtém usuário por ID</summary>
    /// <param name="id">ID do usuário</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(HateoasResponse<UsuarioResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        var usuario = await _ctx.Usuarios
            .Include(u => u.Carros)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario == null)
        {
            _log.LogWarning("Usuário {Id} não encontrado", id);
            return NotFound();
        }

        var response = new HateoasResponse<UsuarioResponse>(_mapper.Map<UsuarioResponse>(usuario))
            .AddLink(Url?.Action(nameof(GetById), new { id }) ?? string.Empty, "self")
            .AddLink(Url?.Action(nameof(Update), new { id }) ?? string.Empty, "update", "PUT")
            .AddLink(Url?.Action(nameof(Delete), new { id }) ?? string.Empty, "delete", "DELETE")
            .AddLink(Url?.Action(nameof(GetAll)) ?? string.Empty, "collection");

        return Ok(response);
    }

    /// <summary>Cria novo usuário</summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(UsuarioRequest request)
    {
        var usuario = _mapper.Map<Usuario>(request);
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        _ctx.Usuarios.Add(usuario);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Usuário criado: {Id} - {Email}", usuario.Id, usuario.Email);
        _bus.SendMessage($"USUARIO_CRIADO|Id:{usuario.Id}|Email:{usuario.Email}");
        await _audit.RegistrarAsync("Usuario", "CRIACAO", usuario.Id, usuario.Email,
            $"Usuário {usuario.Nome} criado.");

        return CreatedAtAction(nameof(GetById), new { id = usuario.Id },
            _mapper.Map<UsuarioResponse>(usuario));
    }

    /// <summary>Atualiza usuário existente</summary>
    /// <param name="id">ID do usuário</param>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, UsuarioRequest request)
    {
        var existing = await _ctx.Usuarios.FindAsync(id);
        if (existing == null) return NotFound();

        _mapper.Map(request, existing);
        await _ctx.SaveChangesAsync();

        _log.LogInformation("Usuário {Id} atualizado.", id);
        _bus.SendMessage($"USUARIO_ATUALIZADO|Id:{id}");
        await _audit.RegistrarAsync("Usuario", "ATUALIZACAO", id, request.Email);

        return NoContent();
    }

    /// <summary>Remove usuário</summary>
    /// <param name="id">ID do usuário</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
            await _audit.RegistrarAsync("Usuario", "REMOCAO", id, usuario.Email);
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            _log.LogError(ex, "Erro ao remover usuário {Id}.", id);
            return BadRequest("Não é possível remover um usuário que possui carros cadastrados.");
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OsFacil.Data;
using OsFacil.DTO.Request;
using OsFacil.DTO.Response;
using OsFacil.Services;

namespace OsFacil.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly TokenService _tokenService;
    private readonly ILogger<AuthController> _log;

    public AuthController(AppDbContext ctx, TokenService tokenService, ILogger<AuthController> log)
    {
        _ctx = ctx;
        _tokenService = tokenService;
        _log = log;
    }

  
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var usuario = await _ctx.Usuarios
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
        {
            _log.LogWarning("Tentativa de login inválida para: {Email}", request.Email);
            return Unauthorized(new { mensagem = "Credenciais inválidas." });
        }

        var (token, expiracao) = _tokenService.GerarToken(usuario);
        _log.LogInformation("Login efetuado: {Email}", usuario.Email);

        return Ok(new LoginResponse(token, usuario.Nome, usuario.Email, expiracao));
    }
}

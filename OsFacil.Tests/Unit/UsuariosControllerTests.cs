using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using AutoMapper;
using OsFacil.Data;
using OsFacil.Models;
using OsFacil.Controllers;
using OsFacil.DTO.Request;
using OsFacil.DTO.Response;
using OsFacil.Messaging;

namespace OsFacil.Tests.Unit;

public class UsuariosControllerTests
{
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<UsuariosController>> _loggerMock;
    private readonly Mock<RabbitMqProducer> _busMock;

    public UsuariosControllerTests()
    {
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<UsuariosController>>();

        
        var configMock = new Mock<IConfiguration>();
        _busMock = new Mock<RabbitMqProducer>(configMock.Object);
    }

    private AppDbContext GetContext()
    {
        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opt);
    }

    [Fact]
    public async Task Create_DadosValidos_SalvaEEnviaMensagem()
    {
      
        var ctx = GetContext();
        var ctrl = new UsuariosController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);

        var request = new UsuarioRequest( "Renato", "renato@teste.com", "123");
        var usuarioEntity = new Usuario { Id = 1, Nome = "Renato", Email = "renato@teste.com" };

        _mapperMock.Setup(m => m.Map<Usuario>(request)).Returns(usuarioEntity);

       
        var result = await ctrl.Create(request);

       
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(1, ctx.Usuarios.Count()); 
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("USUARIO_CRIADO"))), Times.Once); 
    }

    [Fact]
    public async Task GetById_UsuarioInexistente_Retorna404()
    {
        
        var ctx = GetContext();
        var ctrl = new UsuariosController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);

        
        var result = await ctrl.GetById(999);

        
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_UsuarioExiste_RemoveEEnviaMensagem()
    {
        
        var ctx = GetContext();
        var usuario = new Usuario { Id = 1, Nome = "Para Deletar", Email = "del@teste.com" };
        ctx.Usuarios.Add(usuario);
        await ctx.SaveChangesAsync();

        var ctrl = new UsuariosController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);

        
        var result = await ctrl.Delete(1);

       
        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, ctx.Usuarios.Count()); 
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("USUARIO_REMOVIDO"))), Times.Once);
    }

    [Fact]
    public async Task GetAll_ExistemUsuarios_RetornaListaMapeada()
    {
        
        var ctx = GetContext();
        ctx.Usuarios.Add(new Usuario { Nome = "User 1" });
        ctx.Usuarios.Add(new Usuario { Nome = "User 2" });
        await ctx.SaveChangesAsync();

        var usuariosResponse = new List<UsuarioResponse> {
            new UsuarioResponse(1, "Usuario 1", "user1@teste.com", DateTime.Now),
            new UsuarioResponse(2, "Usuario 2", "user2@teste.com", DateTime.Now)
        };
        _mapperMock.Setup(m => m.Map<IEnumerable<UsuarioResponse>>(It.IsAny<IEnumerable<Usuario>>()))
                   .Returns(usuariosResponse);

        var ctrl = new UsuariosController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);

      
        var result = await ctrl.GetAll();

        
        var okResult = Assert.IsType<OkObjectResult>(result);
        var lista = Assert.IsAssignableFrom<IEnumerable<UsuarioResponse>>(okResult.Value);
        Assert.Equal(2, lista.Count());
    }
}

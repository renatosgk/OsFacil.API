using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using OsFacil.Controllers;
using OsFacil.Data;
using OsFacil.DTO.Request;
using OsFacil.DTO.Response;
using OsFacil.Messaging;
using OsFacil.Models;
using OsFacil.MongoDB;

namespace OsFacil.Tests.Unit;

public class UsuariosControllerTests
{
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<UsuariosController>> _loggerMock = new();
    private readonly Mock<RabbitMqProducer> _busMock;
    private readonly Mock<IMongoAuditService> _auditMock = new();

    public UsuariosControllerTests()
    {
        var configMock = new Mock<IConfiguration>();
        _busMock = new Mock<RabbitMqProducer>(configMock.Object);
        _auditMock.Setup(m => m.RegistrarAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(),
            It.IsAny<string?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    private AppDbContext GetContext()
    {
        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opt);
    }

    private UsuariosController CreateController(AppDbContext ctx) =>
        new(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object, _auditMock.Object);

    // --- Arrange / Act / Assert ---

    [Fact]
    public async Task Create_DadosValidos_SalvaEEnviaMensagem()
    {
        // Arrange
        var ctx = GetContext();
        var ctrl = CreateController(ctx);
        var request = new UsuarioRequest("Renato", "renato@teste.com", "123456");
        var entity = new Usuario { Id = 1, Nome = "Renato", Email = "renato@teste.com" };

        _mapperMock.Setup(m => m.Map<Usuario>(request)).Returns(entity);

        // Act
        var result = await ctrl.Create(request);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(1, ctx.Usuarios.Count());
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("USUARIO_CRIADO"))), Times.Once);
    }

    [Fact]
    public async Task GetById_UsuarioInexistente_Retorna404()
    {
        // Arrange
        var ctx = GetContext();
        var ctrl = CreateController(ctx);

        // Act
        var result = await ctrl.GetById(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_UsuarioExiste_RemoveEEnviaMensagem()
    {
        // Arrange
        var ctx = GetContext();
        ctx.Usuarios.Add(new Usuario { Id = 1, Nome = "Para Deletar", Email = "del@teste.com" });
        await ctx.SaveChangesAsync();
        var ctrl = CreateController(ctx);

        // Act
        var result = await ctrl.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, ctx.Usuarios.Count());
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("USUARIO_REMOVIDO"))), Times.Once);
    }

    [Fact]
    public async Task GetAll_ExistemUsuarios_RetornaListaMapeada()
    {
        // Arrange
        var ctx = GetContext();
        ctx.Usuarios.Add(new Usuario { Nome = "User 1" });
        ctx.Usuarios.Add(new Usuario { Nome = "User 2" });
        await ctx.SaveChangesAsync();

        var mockResp = new List<UsuarioResponse>
        {
            new(1, "Usuario 1", "user1@teste.com", DateTime.Now),
            new(2, "Usuario 2", "user2@teste.com", DateTime.Now)
        };
        _mapperMock.Setup(m => m.Map<IEnumerable<UsuarioResponse>>(It.IsAny<IEnumerable<Usuario>>()))
                   .Returns(mockResp);

        var ctrl = CreateController(ctx);

        // Act
        var result = await ctrl.GetAll(new Common.PaginationParams());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}

using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using OsFacil.Common;
using OsFacil.Controllers;
using OsFacil.Data;
using OsFacil.DTO.Request;
using OsFacil.DTO.Response;
using OsFacil.Messaging;
using OsFacil.Models;
using OsFacil.MongoDB;

namespace OsFacil.Tests.Unit;

public class FuncionariosControllerTests
{
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<FuncionariosController>> _loggerMock = new();
    private readonly Mock<RabbitMqProducer> _busMock;
    private readonly Mock<IMongoAuditService> _auditMock = new();

    public FuncionariosControllerTests()
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

    private FuncionariosController CreateController(AppDbContext ctx) =>
        new(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object, _auditMock.Object);

    [Fact]
    public async Task Create_DadosValidos_SalvaEEnviaMensagem()
    {
        // Arrange
        var ctx = GetContext();
        var ctrl = CreateController(ctx);
        var request = new FuncionarioRequest("Renato Mecânico", "Senior", 5000.00m);
        var entity = new Funcionario { Id = 1, Nome = "Renato Mecânico", Cargo = "Senior" };
        _mapperMock.Setup(m => m.Map<Funcionario>(request)).Returns(entity);

        // Act
        var result = await ctrl.Create(request);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(1, ctx.Funcionarios.Count());
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("FUNCIONARIO_CRIADO"))), Times.Once);
    }

    [Fact]
    public async Task Update_FuncionarioExiste_AtualizaEEnviaMensagem()
    {
        // Arrange
        var ctx = GetContext();
        ctx.Funcionarios.Add(new Funcionario { Id = 1, Nome = "Antigo Nome", Cargo = "Junior" });
        await ctx.SaveChangesAsync();
        var ctrl = CreateController(ctx);
        var request = new FuncionarioRequest("Novo Nome", "Pleno", 3500.00m);

        // Act
        var result = await ctrl.Update(1, request);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("FUNCIONARIO_ATUALIZADO"))), Times.Once);
    }

    [Fact]
    public async Task Delete_FuncionarioInexistente_RetornaNotFound()
    {
        // Arrange
        var ctx = GetContext();
        ctx.Funcionarios.Add(new Funcionario { Id = 1, Nome = "Mecânico com OS" });
        await ctx.SaveChangesAsync();
        var ctrl = CreateController(ctx);

        // Act
        var result = await ctrl.Delete(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetAll_ExistemFuncionarios_RetornaPagedResult()
    {
        // Arrange
        var ctx = GetContext();
        ctx.Funcionarios.Add(new Funcionario { Id = 1, Nome = "Func 1" });
        ctx.Funcionarios.Add(new Funcionario { Id = 2, Nome = "Func 2" });
        await ctx.SaveChangesAsync();

        var responseList = new List<FuncionarioResponse>
        {
            new(1, "Func 1", "Mecânico", DateTime.Now),
            new(2, "Func 2", "Auxiliar", DateTime.Now)
        };
        _mapperMock.Setup(m => m.Map<IEnumerable<FuncionarioResponse>>(It.IsAny<IEnumerable<Funcionario>>()))
                   .Returns(responseList);

        var ctrl = CreateController(ctx);

        // Act
        var result = await ctrl.GetAll(new PaginationParams());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}

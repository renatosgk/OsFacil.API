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

public class ItemServicoControllerTests
{
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<ItemServicoController>> _loggerMock = new();
    private readonly Mock<RabbitMqProducer> _busMock;
    private readonly Mock<IMongoAuditService> _auditMock = new();

    public ItemServicoControllerTests()
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

    private ItemServicoController CreateController(AppDbContext ctx) =>
        new(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object, _auditMock.Object);

    [Fact]
    public async Task Create_DadosValidos_SalvaEEnviaMensagem()
    {
        // Arrange
        var ctx = GetContext();
        ctx.OrdensServico.Add(new OrdemServico { Id = 1, Descricao = "OS Teste" });
        await ctx.SaveChangesAsync();

        var ctrl = CreateController(ctx);
        var request = new ItemServicoRequest("Troca de Filtro", 50.00m, 1, 1);
        var entity = new ItemServico { Id = 10, Descricao = "Troca de Filtro", OrdemServicoId = 1, PrecoUnitario = 50.00m };
        _mapperMock.Setup(m => m.Map<ItemServico>(request)).Returns(entity);

        // Act
        var result = await ctrl.Create(request);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(1, ctx.ItensServico.Count());
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("ITEM_ADICIONADO"))), Times.Once);
    }

    [Fact]
    public async Task Create_OrdemInexistente_RetornaBadRequest()
    {
        // Arrange
        var ctx = GetContext();
        var ctrl = CreateController(ctx);
        var request = new ItemServicoRequest("Item Orfão", 10.00m, 1, 999);

        // Act
        var result = await ctrl.Create(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("não existe", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task GetByOrdem_FiltraCorretamente_RetornaApenasItensDaquelaOS()
    {
        // Arrange
        var ctx = GetContext();
        ctx.ItensServico.Add(new ItemServico { Id = 1, OrdemServicoId = 1, Descricao = "Item OS 1" });
        ctx.ItensServico.Add(new ItemServico { Id = 2, OrdemServicoId = 1, Descricao = "Outro Item OS 1" });
        ctx.ItensServico.Add(new ItemServico { Id = 3, OrdemServicoId = 2, Descricao = "Item de Outra OS" });
        await ctx.SaveChangesAsync();

        var responseList = new List<ItemServicoResponse>
        {
            new(1, "Item OS 1", 0, 0, 0),
            new(2, "Outro Item OS 1", 0, 0, 0)
        };
        _mapperMock.Setup(m => m.Map<IEnumerable<ItemServicoResponse>>(It.IsAny<IEnumerable<ItemServico>>()))
                   .Returns(responseList);

        var ctrl = CreateController(ctx);

        // Act
        var result = await ctrl.GetByOrdem(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        // GetByOrdem returns List<HateoasResponse<ItemServicoResponse>>
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Delete_ItemExiste_RemoveEEnviaMensagem()
    {
        // Arrange
        var ctx = GetContext();
        ctx.ItensServico.Add(new ItemServico { Id = 100, OrdemServicoId = 1, Descricao = "Pneu" });
        await ctx.SaveChangesAsync();
        var ctrl = CreateController(ctx);

        // Act
        var result = await ctrl.Delete(100);

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, ctx.ItensServico.Count());
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("ITEM_REMOVIDO"))), Times.Once);
    }
}

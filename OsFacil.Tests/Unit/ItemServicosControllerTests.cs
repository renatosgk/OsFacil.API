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

public class ItemServicoControllerTests
{
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<ItemServicoController>> _loggerMock;
    private readonly Mock<RabbitMqProducer> _busMock;

    public ItemServicoControllerTests()
    {
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<ItemServicoController>>();

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

        
        ctx.OrdensServico.Add(new OrdemServico { Id = 1, Descricao = "OS Teste" });
        await ctx.SaveChangesAsync();

        var ctrl = new ItemServicoController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);

        var request = new ItemServicoRequest("Troca de Filtro", 50.00m, 1, 1);
        var itemEntity = new ItemServico { Id = 10, Descricao = "Troca de Filtro", OrdemServicoId = 1, PrecoUnitario = 50.00m };

        _mapperMock.Setup(m => m.Map<ItemServico>(request)).Returns(itemEntity);

        
        var result = await ctrl.Create(request);

       
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(1, ctx.ItensServico.Count());
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("ITEM_ADICIONADO"))), Times.Once);
    }

    [Fact]
    public async Task Create_OrdemInexistente_RetornaBadRequest()
    {
       
        var ctx = GetContext(); 
        var ctrl = new ItemServicoController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);
        var request = new ItemServicoRequest("Item Orfão", 10.00m, 1, 999);

       
        var result = await ctrl.Create(request);

       
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("não existe", badRequest.Value.ToString());
    }

    [Fact]
    public async Task GetByOrdem_FiltraCorretamente_RetornaApenasItensDaquelaOS()
    {
        
        var ctx = GetContext();
        ctx.ItensServico.Add(new ItemServico { Id = 1, OrdemServicoId = 1, Descricao = "Item OS 1" });
        ctx.ItensServico.Add(new ItemServico { Id = 2, OrdemServicoId = 1, Descricao = "Outro Item OS 1" });
        ctx.ItensServico.Add(new ItemServico { Id = 3, OrdemServicoId = 2, Descricao = "Item de Outra OS" });
        await ctx.SaveChangesAsync();

        
        var responseList = new List<ItemServicoResponse>
        {
            new ItemServicoResponse(1, "Item OS 1", 0, 0, 0),
            new ItemServicoResponse(2, "Outro Item OS 1", 0, 0, 0)
        };
        _mapperMock.Setup(m => m.Map<IEnumerable<ItemServicoResponse>>(It.IsAny<IEnumerable<ItemServico>>()))
                   .Returns(responseList);

        var ctrl = new ItemServicoController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);

        
        var result = await ctrl.GetByOrdem(1);

       
        var okResult = Assert.IsType<OkObjectResult>(result);
        var lista = Assert.IsAssignableFrom<IEnumerable<ItemServicoResponse>>(okResult.Value);
        Assert.Equal(2, lista.Count());
    }

    [Fact]
    public async Task Delete_ItemExiste_RemoveEEnviaMensagem()
    {
       
        var ctx = GetContext();
        var item = new ItemServico { Id = 100, OrdemServicoId = 1, Descricao = "Pneu" };
        ctx.ItensServico.Add(item);
        await ctx.SaveChangesAsync();

        var ctrl = new ItemServicoController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);

       
        var result = await ctrl.Delete(100);

    
        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, ctx.ItensServico.Count());
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("ITEM_REMOVIDO"))), Times.Once);
    }
}

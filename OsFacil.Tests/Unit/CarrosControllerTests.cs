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

public class CarrosControllerTests
{
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<CarrosController>> _loggerMock = new();
    private readonly Mock<RabbitMqProducer> _busMock;
    private readonly Mock<IMongoAuditService> _auditMock = new();

    public CarrosControllerTests()
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

    private CarrosController CreateController(AppDbContext ctx) =>
        new(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object, _auditMock.Object);

    [Fact]
    public async Task Create_DadosValidos_SalvaEEnviaMensagem()
    {
        
        var ctx = GetContext();
        ctx.Usuarios.Add(new Usuario { Id = 1, Nome = "Renato" });
        await ctx.SaveChangesAsync();

        var ctrl = CreateController(ctx);
        var request = new CarroRequest("Honda", "Civic", 2024, "ABC1D23", 1);
        var entity = new Carro { Id = 10, Placa = "ABC1D23", UsuarioId = 1 };
        _mapperMock.Setup(m => m.Map<Carro>(request)).Returns(entity);

       
        var result = await ctrl.Create(request);

       
        Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(1, ctx.Carros.Count());
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("CARRO_CADASTRADO"))), Times.Once);
    }

    [Fact]
    public async Task Create_UsuarioInexistente_RetornaBadRequest()
    {
        
        var ctx = GetContext();
        var ctrl = CreateController(ctx);
        var request = new CarroRequest("Ford", "Ka", 2020, "KKK0K00", 999);

        
        var result = await ctrl.Create(request);

       
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("não existe", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task Delete_CarroExiste_RemoveEEnviaMensagem()
    {
        
        var ctx = GetContext();
        ctx.Carros.Add(new Carro { Id = 50, Placa = "DEL1234" });
        await ctx.SaveChangesAsync();
        var ctrl = CreateController(ctx);

       
        var result = await ctrl.Delete(50);

        
        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, ctx.Carros.Count());
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("CARRO_REMOVIDO"))), Times.Once);
    }

    [Fact]
    public async Task GetById_CarroComUsuario_RetornaHateoas()
    {
        
        var ctx = GetContext();
        var usuario = new Usuario { Id = 1, Nome = "Dono" };
        var carro = new Carro { Id = 1, Placa = "GET1234", Usuario = usuario };
        ctx.Carros.Add(carro);
        await ctx.SaveChangesAsync();

        var carroResp = new CarroResponse(1, "Honda", "Fit", 2015, "GET1234", 1);
        _mapperMock.Setup(m => m.Map<CarroResponse>(It.IsAny<Carro>())).Returns(carroResp);

        var ctrl = CreateController(ctx);

       
        var result = await ctrl.GetById(1);

        
        var okResult = Assert.IsType<OkObjectResult>(result);
        var hateoasResp = Assert.IsType<HateoasResponse<CarroResponse>>(okResult.Value);
        Assert.Equal("GET1234", hateoasResp.Data.Placa);
        Assert.Contains(hateoasResp.Links, l => l.Rel == "self");
    }
}

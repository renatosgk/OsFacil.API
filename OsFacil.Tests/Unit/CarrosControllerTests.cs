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

public class CarrosControllerTests
{
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<CarrosController>> _loggerMock;
    private readonly Mock<RabbitMqProducer> _busMock;

    public CarrosControllerTests()
    {
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<CarrosController>>();

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

        
        var usuario = new Usuario { Id = 1, Nome = "Renato" };
        ctx.Usuarios.Add(usuario);
        await ctx.SaveChangesAsync();

        var ctrl = new CarrosController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);

        var request = new CarroRequest("Honda", "Civic", 2024, "ABC1D23", 1);
        var carroEntity = new Carro { Id = 10, Placa = "ABC1D23", UsuarioId = 1 };

        _mapperMock.Setup(m => m.Map<Carro>(request)).Returns(carroEntity);

      
        var result = await ctrl.Create(request);

       
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(1, ctx.Carros.Count());
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("CARRO_CADASTRADO"))), Times.Once);
    }

    [Fact]
    public async Task Create_UsuarioInexistente_RetornaBadRequest()
    {
        
        var ctx = GetContext();
        var ctrl = new CarrosController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);
        var request = new CarroRequest("Ford", "Ka", 2020, "KKK0K00", 999);

       
        var result = await ctrl.Create(request);

       
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("não existe", badRequest.Value.ToString());
    }

    [Fact]
    public async Task Delete_CarroExiste_RemoveEEnviaMensagem()
    {
        
        var ctx = GetContext();
        var carro = new Carro { Id = 50, Placa = "DEL1234" };
        ctx.Carros.Add(carro);
        await ctx.SaveChangesAsync();

        var ctrl = new CarrosController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);

        
        var result = await ctrl.Delete(50);

        
        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, ctx.Carros.Count());
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("CARRO_REMOVIDO"))), Times.Once);
    }

    [Fact]
    public async Task GetById_CarroComUsuario_RetornaMapeado()
    {
       
        var ctx = GetContext();
        var usuario = new Usuario { Id = 1, Nome = "Dono" };
        var carro = new Carro { Id = 1, Placa = "GET1234", Usuario = usuario };
        ctx.Carros.Add(carro);
        await ctx.SaveChangesAsync();

        var response = new CarroResponse(1, "Honda", "Fit", 2015, "GET1234", 1);
        _mapperMock.Setup(m => m.Map<CarroResponse>(It.IsAny<Carro>())).Returns(response);

        var ctrl = new CarrosController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);

        
        var result = await ctrl.GetById(1);

       
        var okResult = Assert.IsType<OkObjectResult>(result);
        var retorno = Assert.IsType<CarroResponse>(okResult.Value);
        Assert.Equal("GET1234", retorno.Placa);
    }
}
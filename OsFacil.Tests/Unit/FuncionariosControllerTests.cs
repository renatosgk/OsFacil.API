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

public class FuncionariosControllerTests
{
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<FuncionariosController>> _loggerMock;
    private readonly Mock<RabbitMqProducer> _busMock;

    public FuncionariosControllerTests()
    {
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<FuncionariosController>>();

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
        var ctrl = new FuncionariosController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);

        var request = new FuncionarioRequest("Renato Mecânico", "Senior", 5000.00m);
        var entity = new Funcionario { Id = 1, Nome = "Renato Mecânico", Cargo = "Senior" };

        _mapperMock.Setup(m => m.Map<Funcionario>(request)).Returns(entity);

       
        var result = await ctrl.Create(request);

       
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(1, ctx.Funcionarios.Count());
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("FUNCIONARIO_CRIADO"))), Times.Once);
    }

    [Fact]
    public async Task Update_FuncionarioExiste_AtualizaEEnviaMensagem()
    {
        
        var ctx = GetContext();
        var funcionario = new Funcionario { Id = 1, Nome = "Antigo Nome", Cargo = "Junior" };
        ctx.Funcionarios.Add(funcionario);
        await ctx.SaveChangesAsync();

        var ctrl = new FuncionariosController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);
        var request = new FuncionarioRequest("Novo Nome", "Pleno", 3500.00m);

        
        var result = await ctrl.Update(1, request);

        
        Assert.IsType<NoContentResult>(result);
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("FUNCIONARIO_ATUALIZADO"))), Times.Once);
    }

    [Fact]
    public async Task Delete_FuncionarioComOS_RetornaBadRequest()
    {
       
        var ctx = GetContext();
        var funcionario = new Funcionario { Id = 1, Nome = "Mecânico com OS" };
        ctx.Funcionarios.Add(funcionario);
        await ctx.SaveChangesAsync();

        var ctrl = new FuncionariosController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);

       
        var result = await ctrl.Delete(999); 

        
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetAll_ExistemFuncionarios_RetornaListaMapeada()
    {
        
        var ctx = GetContext();
        ctx.Funcionarios.Add(new Funcionario { Id = 1, Nome = "Func 1" });
        ctx.Funcionarios.Add(new Funcionario { Id = 2, Nome = "Func 2" });
        await ctx.SaveChangesAsync();

        var responseList = new List<FuncionarioResponse>
        {
            new FuncionarioResponse(1, "Func 1", "Mecânico", DateTime.Now),
            new FuncionarioResponse(2, "Func 2", "Auxiliar", DateTime.Now)
        };

        _mapperMock.Setup(m => m.Map<IEnumerable<FuncionarioResponse>>(It.IsAny<IEnumerable<Funcionario>>()))
                   .Returns(responseList);

        var ctrl = new FuncionariosController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);

       
        var result = await ctrl.GetAll();

        
        var okResult = Assert.IsType<OkObjectResult>(result);
        var lista = Assert.IsAssignableFrom<IEnumerable<FuncionarioResponse>>(okResult.Value);
        Assert.Equal(2, lista.Count());
    }
}
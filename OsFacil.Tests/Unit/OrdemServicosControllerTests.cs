
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
using OsFacil.Enum;
using OsFacil.Messaging;
using OsFacil.Models;
using System.Net.NetworkInformation;

namespace OsFacil.Tests.Unit;

public class OrdemServicoControllerTests
{
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<OrdemServicoController>> _loggerMock;
    private readonly Mock<RabbitMqProducer> _busMock;

    public OrdemServicoControllerTests()
    {
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<OrdemServicoController>>();

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

       
        ctx.Usuarios.Add(new Usuario { Id = 1, Nome = "Cliente" });
        ctx.Funcionarios.Add(new Funcionario { Id = 1, Nome = "Mecânico" });
        ctx.Carros.Add(new Carro { Id = 1, Placa = "ABC1234" });
        await ctx.SaveChangesAsync();

        var ctrl = new OrdemServicoController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);

        var request = new OrdemServicoRequest(
            "Troca de Óleo", 150.00m, 1, 1, 1, StatusOS.Aprovado
        );

        var osEntity = new OrdemServico { Id = 10, Descricao = "Troca de Óleo", Valor = 150.00m };
        _mapperMock.Setup(m => m.Map<OrdemServico>(request)).Returns(osEntity);

        
        var result = await ctrl.Create(request);

       
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(1, ctx.OrdensServico.Count());
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("OS_CRIADA"))), Times.Once);
    }

    [Fact]
    public async Task Create_UsuarioInexistente_RetornaBadRequest()
    {
        var ctx = GetContext();
        var ctrl = new OrdemServicoController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);

     
        var request = new OrdemServicoRequest("Erro", 100, 99, 1, 1, StatusOS.EmExecucao);

       
        var result = await ctrl.Create(request);

        
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
       
        Assert.Contains("UsuarioId", badRequest.Value.ToString());
    }

    [Fact]
    public async Task UpdateStatus_OSExiste_AtualizaEEnviaMensagem()
    {
        var ctx = GetContext();
        var os = new OrdemServico { Id = 5, Status = StatusOS.EmExecucao };
        ctx.OrdensServico.Add(os);
        await ctx.SaveChangesAsync();

        var ctrl = new OrdemServicoController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);

     
        var result = await ctrl.UpdateStatus(5, StatusOS.Concluido);

        
        Assert.IsType<NoContentResult>(result);

      
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("OS_STATUS_ALTERADO"))), Times.Once);
    }

    [Fact]
    public async Task GetById_OSExiste_RetornaMapeado()
    {
        using var ctx = GetContext();

      
        var usuario = new Usuario { Id = 1, Nome = "Cliente Teste", Email = "teste@teste.com" };
        var carro = new Carro { Id = 1, Placa = "ABC1234", Marca = "Teste", Modelo = "Teste" };
        var funcionario = new Funcionario { Id = 1, Nome = "Mecânico Teste", Cargo = "Mecânico" };

        ctx.Usuarios.Add(usuario);
        ctx.Carros.Add(carro);
        ctx.Funcionarios.Add(funcionario);

        
        var os = new OrdemServico
        {
            Id = 1,
            Descricao = "Conserto Teste",
            UsuarioId = 1,
            CarroId = 1,
            FuncionarioId = 1,
            Status = StatusOS.EmExecucao
        };

        ctx.OrdensServico.Add(os);
        await ctx.SaveChangesAsync(); 

       
        var response = new OrdemServicoResponse(1, "Conserto Teste", 0, DateTime.Now, 1, 1, 1, "ABERTA");
        _mapperMock.Setup(m => m.Map<OrdemServicoResponse>(It.IsAny<OrdemServico>()))
                   .Returns(response);

        var ctrl = new OrdemServicoController(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object);

        
        var result = await ctrl.GetById(1); 

        
        var okResult = Assert.IsType<OkObjectResult>(result); 
        var retorno = Assert.IsType<OrdemServicoResponse>(okResult.Value);
        Assert.Equal("Conserto Teste", retorno.Descricao);
    }
}

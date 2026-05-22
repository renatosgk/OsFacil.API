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
using OsFacil.Enum;
using OsFacil.Messaging;
using OsFacil.Models;
using OsFacil.MongoDB;

namespace OsFacil.Tests.Unit;

public class OrdemServicoControllerTests
{
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<OrdemServicoController>> _loggerMock = new();
    private readonly Mock<RabbitMqProducer> _busMock;
    private readonly Mock<IMongoAuditService> _auditMock = new();

    public OrdemServicoControllerTests()
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

    private OrdemServicoController CreateController(AppDbContext ctx) =>
        new(ctx, _mapperMock.Object, _loggerMock.Object, _busMock.Object, _auditMock.Object);

    [Fact]
    public async Task Create_DadosValidos_SalvaEEnviaMensagem()
    {
       
        var ctx = GetContext();
        ctx.Usuarios.Add(new Usuario { Id = 1, Nome = "Cliente" });
        ctx.Funcionarios.Add(new Funcionario { Id = 1, Nome = "Mecânico" });
        ctx.Carros.Add(new Carro { Id = 1, Placa = "ABC1234" });
        await ctx.SaveChangesAsync();

        var ctrl = CreateController(ctx);
        var request = new OrdemServicoRequest("Troca de Óleo", 150.00m, 1, 1, 1, StatusOS.Aprovado);
        var osEntity = new OrdemServico { Id = 10, Descricao = "Troca de Óleo", Valor = 150.00m };
        _mapperMock.Setup(m => m.Map<OrdemServico>(request)).Returns(osEntity);

       
        var result = await ctrl.Create(request);

        
        Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(1, ctx.OrdensServico.Count());
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("OS_CRIADA"))), Times.Once);
    }

    [Fact]
    public async Task Create_UsuarioInexistente_RetornaBadRequest()
    {
        
        var ctx = GetContext();
        var ctrl = CreateController(ctx);
        var request = new OrdemServicoRequest("Erro", 100, 99, 1, 1, StatusOS.EmExecucao);

       
        var result = await ctrl.Create(request);

       
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("UsuarioId", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task UpdateStatus_OSExiste_AtualizaEEnviaMensagem()
    {
       
        var ctx = GetContext();
        ctx.OrdensServico.Add(new OrdemServico { Id = 5, Status = StatusOS.EmExecucao });
        await ctx.SaveChangesAsync();
        var ctrl = CreateController(ctx);

        
        var result = await ctrl.UpdateStatus(5, StatusOS.Concluido);

       
        Assert.IsType<NoContentResult>(result);
        _busMock.Verify(b => b.SendMessage(It.Is<string>(s => s.Contains("OS_STATUS_ALTERADO"))), Times.Once);
    }

    [Fact]
    public async Task GetById_OSExiste_RetornaHateoas()
    {
        
        using var ctx = GetContext();
        ctx.Usuarios.Add(new Usuario { Id = 1, Nome = "Cliente Teste", Email = "teste@teste.com" });
        ctx.Carros.Add(new Carro { Id = 1, Placa = "ABC1234", Marca = "Teste", Modelo = "Teste" });
        ctx.Funcionarios.Add(new Funcionario { Id = 1, Nome = "Mecânico Teste", Cargo = "Mecânico" });
        ctx.OrdensServico.Add(new OrdemServico
        {
            Id = 1, Descricao = "Conserto Teste",
            UsuarioId = 1, CarroId = 1, FuncionarioId = 1, Status = StatusOS.EmExecucao
        });
        await ctx.SaveChangesAsync();

        var response = new OrdemServicoResponse(1, "Conserto Teste", 0, DateTime.Now, 1, 1, 1, "EmExecucao");
        _mapperMock.Setup(m => m.Map<OrdemServicoResponse>(It.IsAny<OrdemServico>())).Returns(response);

        var ctrl = CreateController(ctx);

        
        var result = await ctrl.GetById(1);

       
        var okResult = Assert.IsType<OkObjectResult>(result);
        var hateoasResp = Assert.IsType<HateoasResponse<OrdemServicoResponse>>(okResult.Value);
        Assert.Equal("Conserto Teste", hateoasResp.Data.Descricao);
        Assert.Contains(hateoasResp.Links, l => l.Rel == "self");
    }
}

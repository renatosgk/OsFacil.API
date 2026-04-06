using System.Net;
using System.Net.Http.Json;
using OsFacil.DTO.Request;
using OsFacil.DTO.Response;
using OsFacil.Enum;

namespace OsFacil.Tests.Integration;

public class ItemServicoIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ItemServicoIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_ItemValido_DeveRetornar201Created()
    {
       
        var userRes = await _client.PostAsJsonAsync("/api/usuarios", new UsuarioRequest("Cliente Item", "item@teste.com", "123"));
        var user = await userRes.Content.ReadFromJsonAsync<UsuarioResponse>();

        var funcRes = await _client.PostAsJsonAsync("/api/funcionarios", new FuncionarioRequest("Mecanico", "Junior", 3000));
        var func = await funcRes.Content.ReadFromJsonAsync<FuncionarioResponse>();

        var carroRes = await _client.PostAsJsonAsync("/api/carros", new CarroRequest("Ford", "Ka", 2018, "CCC1234", user.Id));
        var carro = await carroRes.Content.ReadFromJsonAsync<CarroResponse>();

        var osReq = new OrdemServicoRequest("Manutenção", 0, user.Id, func.Id, carro.Id, StatusOS.EmExecucao);
        var osRes = await _client.PostAsJsonAsync("/api/ordemservico", osReq);
        var os = await osRes.Content.ReadFromJsonAsync<OrdemServicoResponse>();

        
        var itemReq = new ItemServicoRequest("Troca de Vela", 50.00m, 4, os.Id);
        var response = await _client.PostAsJsonAsync("/api/itemservico", itemReq);

       
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var criado = await response.Content.ReadFromJsonAsync<ItemServicoResponse>();
        Assert.NotNull(criado);
        Assert.Equal(200.00m, criado.ValorTotal); 
    }

    [Fact]
    public async Task FluxoCompleto_GerenciamentoDeItens_CriarAtualizarEDeletar()
    {
        
        var userRes = await _client.PostAsJsonAsync("/api/usuarios", new UsuarioRequest("User Fluxo", "fluxo_item@os.com", "123"));
        var user = await userRes.Content.ReadFromJsonAsync<UsuarioResponse>();
        var funcRes = await _client.PostAsJsonAsync("/api/funcionarios", new FuncionarioRequest("Mec Fluxo", "Pleno", 4000));
        var func = await funcRes.Content.ReadFromJsonAsync<FuncionarioResponse>();
        var carroRes = await _client.PostAsJsonAsync("/api/carros", new CarroRequest("VW", "Gol", 2015, "GOL1010", user.Id));
        var carro = await carroRes.Content.ReadFromJsonAsync<CarroResponse>();
        var osRes = await _client.PostAsJsonAsync("/api/ordemservico", new OrdemServicoRequest("OS Fluxo", 0, user.Id, func.Id, carro.Id, StatusOS.EmExecucao));
        var os = await osRes.Content.ReadFromJsonAsync<OrdemServicoResponse>();

        
        var itemRes = await _client.PostAsJsonAsync("/api/itemservico", new ItemServicoRequest("Pastilha", 100, 2, os.Id));
        var item = await itemRes.Content.ReadFromJsonAsync<ItemServicoResponse>();

       
        var updateReq = new ItemServicoRequest("Pastilha", 100, 3, os.Id);
        var putRes = await _client.PutAsJsonAsync($"/api/itemservico/{item.Id}", updateReq);
        Assert.Equal(HttpStatusCode.NoContent, putRes.StatusCode);

      
        var getItensRes = await _client.GetAsync($"/api/itemservico/ordem/{os.Id}");
        var itens = await getItensRes.Content.ReadFromJsonAsync<List<ItemServicoResponse>>();
        Assert.Equal(300.00m, itens[0].ValorTotal); 

        
        var delRes = await _client.DeleteAsync($"/api/itemservico/{item.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delRes.StatusCode);
    }

    [Fact]
    public async Task Post_ItemEmOSInexistente_DeveRetornar400()
    {
        var request = new ItemServicoRequest("Erro", 50, 1, 9999);
        var response = await _client.PostAsJsonAsync("/api/itemservico", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ItemInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync("/api/itemservico/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
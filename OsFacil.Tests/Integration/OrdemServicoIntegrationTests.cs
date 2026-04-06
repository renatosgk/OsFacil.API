using System.Net;
using System.Net.Http.Json;
using OsFacil.DTO.Request;
using OsFacil.DTO.Response;
using OsFacil.Enum;

namespace OsFacil.Tests.Integration;

public class OrdemServicoIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrdemServicoIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_OSValida_DeveRetornar201Created()
    {
       
        var userRes = await _client.PostAsJsonAsync("/api/usuarios", new UsuarioRequest("Dono Teste", "dono@teste.com", "123"));
        var user = await userRes.Content.ReadFromJsonAsync<UsuarioResponse>();

        var funcRes = await _client.PostAsJsonAsync("/api/funcionarios", new FuncionarioRequest("Mecanico Teste", "Senior", 5000));
        var func = await funcRes.Content.ReadFromJsonAsync<FuncionarioResponse>();

        var carroRes = await _client.PostAsJsonAsync("/api/carros", new CarroRequest("Ford", "Ka", 2020, "TST1234", user.Id));
        var carro = await carroRes.Content.ReadFromJsonAsync<CarroResponse>();

     
        var osRequest = new OrdemServicoRequest("Troca de Óleo", 150, user.Id, func.Id, carro.Id, StatusOS.EmExecucao);
        var response = await _client.PostAsJsonAsync("/api/ordemservico", osRequest);

       
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var osCriada = await response.Content.ReadFromJsonAsync<OrdemServicoResponse>();
        Assert.NotNull(osCriada);
        Assert.Equal("Troca de Óleo", osCriada.Descricao);
    }

    [Fact]
    public async Task FluxoGerenciamento_CriarAtualizarEStatus_DeveFuncionarCorretamente()
    {
        
        var userRes = await _client.PostAsJsonAsync("/api/usuarios", new UsuarioRequest("User Fluxo", "fluxo@os.com", "123"));
        var user = await userRes.Content.ReadFromJsonAsync<UsuarioResponse>();
        var funcRes = await _client.PostAsJsonAsync("/api/funcionarios", new FuncionarioRequest("Func Fluxo", "Mestre", 6000));
        var func = await funcRes.Content.ReadFromJsonAsync<FuncionarioResponse>();
        var carroRes = await _client.PostAsJsonAsync("/api/carros", new CarroRequest("Fiat", "Uno", 2010, "FLX1234", user.Id));
        var carro = await carroRes.Content.ReadFromJsonAsync<CarroResponse>();

        var osRequest = new OrdemServicoRequest("Revisão Geral", 500, user.Id, func.Id, carro.Id, StatusOS.EmExecucao);
        var postRes = await _client.PostAsJsonAsync("/api/ordemservico", osRequest);
        var os = await postRes.Content.ReadFromJsonAsync<OrdemServicoResponse>();

        
        var updateReq = new OrdemServicoRequest("Revisão + Filtro", 600, user.Id, func.Id, carro.Id, StatusOS.EmExecucao);
        var putRes = await _client.PutAsJsonAsync($"/api/ordemservico/{os.Id}", updateReq);
        Assert.Equal(HttpStatusCode.NoContent, putRes.StatusCode);

        
        var patchRes = await _client.PatchAsJsonAsync($"/api/ordemservico/{os.Id}/status", StatusOS.Concluido);
        Assert.Equal(HttpStatusCode.NoContent, patchRes.StatusCode);

       
        var getRes = await _client.GetAsync($"/api/ordemservico/{os.Id}");
        var osFinal = await getRes.Content.ReadFromJsonAsync<OrdemServicoResponse>();
        Assert.Equal(StatusOS.Concluido.ToString(), osFinal.Status);
    }

    [Fact]
    public async Task Post_OSComReferenciasInvalidas_DeveRetornar400()
    {
        var request = new OrdemServicoRequest("Erro", 100, 999, 999, 999, StatusOS.EmExecucao);
        var response = await _client.PostAsJsonAsync("/api/ordemservico", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_OSInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync("/api/ordemservico/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
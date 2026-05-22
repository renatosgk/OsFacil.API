using System.Net;
using System.Net.Http.Json;
using OsFacil.Common;
using OsFacil.DTO.Request;
using OsFacil.DTO.Response;
using OsFacil.Tests.Helpers;

namespace OsFacil.Tests.Integration;

public class FuncionariosIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FuncionariosIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _client.AddBearerToken();
    }

    [Fact]
    public async Task Post_FuncionarioValido_DeveRetornar201Created()
    {
        
        var response = await _client.PostAsJsonAsync("/api/funcionarios",
            new FuncionarioRequest("Mecânico de Teste", "Senior", 5500.00m));

        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var criado = await response.Content.ReadFromJsonAsync<FuncionarioResponse>();
        Assert.NotNull(criado);
        Assert.Equal("Mecânico de Teste", criado.Nome);
        Assert.Equal("Senior", criado.Cargo);
    }

    [Fact]
    public async Task FluxoCompleto_GerenciamentoDeFuncionario_CriarAtualizarEDeletar()
    {
       
        var postRes = await _client.PostAsJsonAsync("/api/funcionarios",
            new FuncionarioRequest("Carlos Silva", "Auxiliar", 2500.00m));
        var criado = await postRes.Content.ReadFromJsonAsync<FuncionarioResponse>();

        
        var putRes = await _client.PutAsJsonAsync($"/api/funcionarios/{criado!.Id}",
            new FuncionarioRequest("Carlos Silva", "Mecânico", 3500.00m));
        Assert.Equal(HttpStatusCode.NoContent, putRes.StatusCode);

        
        var getRes = await _client.GetAsync($"/api/funcionarios/{criado.Id}");
        var hateoas = await getRes.Content.ReadFromJsonAsync<HateoasResponse<FuncionarioResponse>>();
        Assert.Equal("Mecânico", hateoas!.Data.Cargo);

        
        var delRes = await _client.DeleteAsync($"/api/funcionarios/{criado.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delRes.StatusCode);
    }

    [Fact]
    public async Task GetById_FuncionarioInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync("/api/funcionarios/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_FuncionarioInexistente_DeveRetornar404()
    {
        var response = await _client.PutAsJsonAsync("/api/funcionarios/99999",
            new FuncionarioRequest("Fantasma", "Cargo", 1000));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_FuncionarioInexistente_DeveRetornar404()
    {
        var response = await _client.DeleteAsync("/api/funcionarios/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

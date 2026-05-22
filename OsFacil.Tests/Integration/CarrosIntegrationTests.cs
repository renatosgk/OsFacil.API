using System.Net;
using System.Net.Http.Json;
using OsFacil.Common;
using OsFacil.DTO.Request;
using OsFacil.DTO.Response;
using OsFacil.Tests.Helpers;

namespace OsFacil.Tests.Integration;

public class CarrosIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CarrosIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _client.AddBearerToken();
    }

    [Fact]
    public async Task FluxoCompleto_CriarCarroValido_DeveRetornar201EPersistir()
    {
       
        var userRes = await _client.PostAsJsonAsync("/api/usuarios",
            new UsuarioRequest("Dono do Carro", "dono@teste.com", "senha123"));
        var user = await userRes.Content.ReadFromJsonAsync<UsuarioResponse>();

        
        var postResponse = await _client.PostAsJsonAsync("/api/carros",
            new CarroRequest("Honda", "Civic", 2024, "ABC1D23", user!.Id));

        
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var carroCriado = await postResponse.Content.ReadFromJsonAsync<CarroResponse>();
        Assert.NotNull(carroCriado);
        Assert.Equal("ABC1D23", carroCriado.Placa);

        
        var getResponse = await _client.GetAsync("/api/carros");
        var pagedList = await getResponse.Content.ReadFromJsonAsync<PagedResult<HateoasResponse<CarroResponse>>>();
        Assert.Contains(pagedList!.Data, c => c.Data.Id == carroCriado.Id);
    }

    [Fact]
    public async Task Post_CarroComUsuarioInexistente_DeveRetornar400()
    {
      
        var response = await _client.PostAsJsonAsync("/api/carros",
            new CarroRequest("Ford", "Ka", 2020, "KKK0K00", 9999));

       
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_CarroInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync("/api/carros/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_CarroExistente_DeveRetornar204()
    {
        
        var userRes = await _client.PostAsJsonAsync("/api/usuarios",
            new UsuarioRequest("Dono Deletar", "del@carro.com", "senha123"));
        Assert.Equal(HttpStatusCode.Created, userRes.StatusCode);
        var user = await userRes.Content.ReadFromJsonAsync<UsuarioResponse>();

        var carroRes = await _client.PostAsJsonAsync("/api/carros",
            new CarroRequest("Fiat", "Palio", 2012, "DEL1234", user!.Id));
        Assert.Equal(HttpStatusCode.Created, carroRes.StatusCode);
        var carroJson = await carroRes.Content.ReadAsStringAsync();
        var carro = System.Text.Json.JsonSerializer.Deserialize<CarroResponse>(carroJson,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

       
        var response = await _client.DeleteAsync($"/api/carros/{carro!.Id}");

       
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}

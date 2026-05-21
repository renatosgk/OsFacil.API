using System.Net;
using System.Net.Http.Json;
using OsFacil.Common;
using OsFacil.DTO.Request;
using OsFacil.DTO.Response;
using OsFacil.Enum;
using OsFacil.Tests.Helpers;

namespace OsFacil.Tests.Integration;

public class OrdemServicoIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrdemServicoIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _client.AddBearerToken();
    }

    private async Task<(UsuarioResponse user, FuncionarioResponse func, CarroResponse carro)>
        CriarPreRequisitos(string placa, string email)
    {
        var userRes = await _client.PostAsJsonAsync("/api/usuarios",
            new UsuarioRequest("Dono", email, "senha123"));
        var user = await userRes.Content.ReadFromJsonAsync<UsuarioResponse>();

        var funcRes = await _client.PostAsJsonAsync("/api/funcionarios",
            new FuncionarioRequest("Mecanico Teste", "Senior", 5000));
        var func = await funcRes.Content.ReadFromJsonAsync<FuncionarioResponse>();

        var carroRes = await _client.PostAsJsonAsync("/api/carros",
            new CarroRequest("Ford", "Ka", 2020, placa, user!.Id));
        var carro = await carroRes.Content.ReadFromJsonAsync<CarroResponse>();

        return (user, func!, carro!);
    }

    [Fact]
    public async Task Post_OSValida_DeveRetornar201Created()
    {
        // Arrange
        var (user, func, carro) = await CriarPreRequisitos("TST1234", "dono@teste.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/ordemservico",
            new OrdemServicoRequest("Troca de Óleo", 150, user.Id, func.Id, carro.Id, StatusOS.EmExecucao));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var osCriada = await response.Content.ReadFromJsonAsync<OrdemServicoResponse>();
        Assert.NotNull(osCriada);
        Assert.Equal("Troca de Óleo", osCriada.Descricao);
    }

    [Fact]
    public async Task FluxoGerenciamento_CriarAtualizarEStatus_DeveFuncionarCorretamente()
    {
        // Arrange
        var (user, func, carro) = await CriarPreRequisitos("FLX1234", "fluxo@os.com");

        var postRes = await _client.PostAsJsonAsync("/api/ordemservico",
            new OrdemServicoRequest("Revisão Geral", 500, user.Id, func.Id, carro.Id, StatusOS.EmExecucao));
        var os = await postRes.Content.ReadFromJsonAsync<OrdemServicoResponse>();

        // Act - atualizar
        var putRes = await _client.PutAsJsonAsync($"/api/ordemservico/{os!.Id}",
            new OrdemServicoRequest("Revisão + Filtro", 600, user.Id, func.Id, carro.Id, StatusOS.EmExecucao));
        Assert.Equal(HttpStatusCode.NoContent, putRes.StatusCode);

        // Act - mudar status
        var patchRes = await _client.PatchAsJsonAsync($"/api/ordemservico/{os.Id}/status", StatusOS.Concluido);
        Assert.Equal(HttpStatusCode.NoContent, patchRes.StatusCode);

        // Assert - GetById retorna HATEOAS
        var getRes = await _client.GetAsync($"/api/ordemservico/{os.Id}");
        var hateoas = await getRes.Content.ReadFromJsonAsync<HateoasResponse<OrdemServicoResponse>>();
        Assert.Equal(StatusOS.Concluido.ToString(), hateoas!.Data.Status);
        Assert.Contains(hateoas.Links, l => l.Rel == "self");
    }

    [Fact]
    public async Task Post_OSComReferenciasInvalidas_DeveRetornar400()
    {
        var response = await _client.PostAsJsonAsync("/api/ordemservico",
            new OrdemServicoRequest("Erro", 100, 999, 999, 999, StatusOS.EmExecucao));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_OSInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync("/api/ordemservico/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

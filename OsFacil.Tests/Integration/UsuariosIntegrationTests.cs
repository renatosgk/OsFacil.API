using System.Net;
using System.Net.Http.Json;
using OsFacil.Common;
using OsFacil.DTO.Request;
using OsFacil.DTO.Response;
using OsFacil.Tests.Helpers;

namespace OsFacil.Tests.Integration;

public class UsuariosIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsuariosIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _client.AddBearerToken();
    }

    [Fact]
    public async Task Post_UsuarioValido_DeveRetornar201Created()
    {
        // Arrange
        var novoUsuario = new UsuarioRequest("Renato Santos", "renato.santos@teste.com", "senha123");

        // Act
        var response = await _client.PostAsJsonAsync("/api/usuarios", novoUsuario);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var criado = await response.Content.ReadFromJsonAsync<UsuarioResponse>();
        Assert.NotNull(criado);
        Assert.Equal("Renato Santos", criado.Nome);
    }

    [Fact]
    public async Task PostEGet_UsuarioPersistido_DeveRetornar200OK()
    {
        // Arrange
        var novoUsuario = new UsuarioRequest("Persistencia Teste", "per@teste.com", "123456");
        await _client.PostAsJsonAsync("/api/usuarios", novoUsuario);

        // Act
        var getResponse = await _client.GetAsync("/api/usuarios");
        var pagedResult = await getResponse.Content.ReadFromJsonAsync<PagedResult<HateoasResponse<UsuarioResponse>>>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Contains(pagedResult!.Data, u => u.Data.Email == "per@teste.com");
    }

    [Fact]
    public async Task Get_UsuarioInexistente_DeveRetornar404NotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/usuarios/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_UsuarioExistente_DeveRetornar204NoContent()
    {
        // Arrange
        var postRes = await _client.PostAsJsonAsync("/api/usuarios",
            new UsuarioRequest("Para Deletar", "del@teste.com", "123456"));
        var criado = await postRes.Content.ReadFromJsonAsync<UsuarioResponse>();

        // Act
        var delRes = await _client.DeleteAsync($"/api/usuarios/{criado!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, delRes.StatusCode);
    }
}

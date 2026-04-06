using System.Net;
using System.Net.Http.Json;
using OsFacil.DTO.Request;
using OsFacil.DTO.Response;

namespace OsFacil.Tests.Integration;

public class UsuariosIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsuariosIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_UsuarioValido_DeveRetornar201Created()
    {
       
        var novoUsuario = new UsuarioRequest("Renato Santos", "renato.santos@teste.com", "senha123");

       
        var response = await _client.PostAsJsonAsync("/api/usuarios", novoUsuario);

        
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        
        var criado = await response.Content.ReadFromJsonAsync<UsuarioResponse>();
        Assert.NotNull(criado);
        Assert.Equal("Renato Santos", criado.Nome);
    }

    [Fact]
    public async Task PostEGet_UsuarioPersistido_DeveRetornar200OK()
    {
        
        var novoUsuario = new UsuarioRequest("Persistencia Teste", "per@teste.com", "123");

      
        await _client.PostAsJsonAsync("/api/usuarios", novoUsuario);

      
        var getResponse = await _client.GetAsync("/api/usuarios");
        var usuarios = await getResponse.Content.ReadFromJsonAsync<List<UsuarioResponse>>();

       
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Contains(usuarios, u => u.Email == "per@teste.com");
    }

    [Fact]
    public async Task Get_UsuarioInexistente_DeveRetornar404NotFound()
    {
        
        var response = await _client.GetAsync("/api/usuarios/99999");

      
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_UsuarioExistente_DeveRetornar204NoContent()
    {
      
        var postRes = await _client.PostAsJsonAsync("/api/usuarios", new UsuarioRequest("Para Deletar", "del@teste.com", "123"));
        var criado = await postRes.Content.ReadFromJsonAsync<UsuarioResponse>();

        
        var delRes = await _client.DeleteAsync($"/api/usuarios/{criado.Id}");

        
        Assert.Equal(HttpStatusCode.NoContent, delRes.StatusCode);
    }
}
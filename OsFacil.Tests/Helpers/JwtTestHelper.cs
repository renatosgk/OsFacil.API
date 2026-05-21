using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OsFacil.Tests.Helpers;

public static class JwtTestHelper
{
    private const string Key = "OsFacil@SuperSecretKey#2026$FIAP!NetCore8";
    private const string Issuer = "OsFacilAPI";
    private const string Audience = "OsFacilClients";

    public static string GerarToken(long userId = 1, string nome = "Teste", string email = "test@teste.com")
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, nome),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static void AddBearerToken(this HttpClient client)
    {
        var token = GerarToken();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}

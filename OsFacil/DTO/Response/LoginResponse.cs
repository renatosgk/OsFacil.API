namespace OsFacil.DTO.Response;

public record LoginResponse(string Token, string Nome, string Email, DateTime Expiracao);

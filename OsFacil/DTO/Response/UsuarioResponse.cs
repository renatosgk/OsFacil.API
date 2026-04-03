namespace OsFacil.DTO.Response
{
    public record UsuarioResponse(
        long Id,
        string Nome,
        string Email,
        DateTime CriadoEm
     );
    
}

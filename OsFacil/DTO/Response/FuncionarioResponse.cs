namespace OsFacil.DTO.Response
{
    public record FuncionarioResponse(
        long Id,
        string Nome,
        string Cargo,
        DateTime DataAdmissao
     );
    
}

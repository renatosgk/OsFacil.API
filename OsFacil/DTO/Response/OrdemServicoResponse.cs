namespace OsFacil.DTO.Response
{
    public record OrdemServicoResponse(
        long Id,
        string Descricao,
        decimal Valor,
        DateTime DataCriacao,
        long CarroId,
        long UsuarioId,
        long FuncionarioId,
        string Status
     );
    
}

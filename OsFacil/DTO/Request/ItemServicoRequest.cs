namespace OsFacil.DTO.Request
{
    public record ItemServicoRequest(
        string Descricao,
        decimal PrecoUnitario,
        decimal Quantidade,
        long OrdemServicoId
     );
    
    
}

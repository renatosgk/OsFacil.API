namespace OsFacil.DTO.Response
{
    public record ItemServicoResponse(
        long Id,
        string Descricao,
        decimal PrecoUnitario,
        decimal Quantidade,
        decimal ValorTotal
     );
    
    
}

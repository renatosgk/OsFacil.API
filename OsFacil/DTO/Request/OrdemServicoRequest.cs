using OsFacil.Enum;

namespace OsFacil.DTO.Request
{
    public record OrdemServicoRequest(
        string Descricao,
        decimal Valor,
        long UsuarioId,
        long FuncionarioId,
        long CarroId,
        StatusOS Status
    );
    
}

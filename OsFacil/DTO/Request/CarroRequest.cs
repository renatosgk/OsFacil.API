namespace OsFacil.DTO.Request
{
    public record CarroRequest(
        string Marca,
        string Modelo,
        int Ano,
        string Placa,
        long UsuarioId
    );

}

namespace OsFacil.DTO.Response
{
    public record CarroResponse(
        long Id,
        string Marca,
        string Modelo,
        int Ano,
        string Placa,
        long UsuarioId
    );
   
}

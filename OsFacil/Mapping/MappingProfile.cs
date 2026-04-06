using AutoMapper;
using OsFacil.DTO.Request;
using OsFacil.DTO.Response;
using OsFacil.Models;

namespace OsFacil.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
      
        CreateMap<Usuario, UsuarioResponse>();
        CreateMap<UsuarioRequest, Usuario>();

        
        CreateMap<Carro, CarroResponse>();
        CreateMap<CarroRequest, Carro>();

        
        CreateMap<Funcionario, FuncionarioResponse>();
        CreateMap<FuncionarioRequest, Funcionario>();

       
        CreateMap<OrdemServico, OrdemServicoResponse>()
            .ConstructUsing(src => new OrdemServicoResponse(
                src.Id,
                src.Descricao,
                src.Valor, 
                src.DataCriacao,
                src.CarroId,
                src.UsuarioId,
                src.FuncionarioId,
                src.Status.ToString()
            ))
            .ForAllMembers(opt => opt.Ignore()); 

        CreateMap<OrdemServicoRequest, OrdemServico>();

     
        CreateMap<ItemServico, ItemServicoResponse>()
            .ConstructUsing(src => new ItemServicoResponse(
                src.Id,
                src.Descricao,
                src.PrecoUnitario,
                src.Quantidade,
                src.PrecoUnitario * src.Quantidade
            ))
            .ForAllMembers(opt => opt.Ignore()); 

        CreateMap<ItemServicoRequest, ItemServico>();
    }
}
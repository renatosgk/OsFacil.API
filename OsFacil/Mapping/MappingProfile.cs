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
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        CreateMap<OrdemServicoRequest, OrdemServico>();

       
        CreateMap<ItemServico, ItemServicoResponse>()
            .ForMember(dest => dest.ValorTotal, opt => opt.MapFrom(src => src.PrecoUnitario * src.Quantidade));
        CreateMap<ItemServicoRequest, ItemServico>();
    }
}
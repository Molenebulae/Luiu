using AutoMapper;
using Luiu.Domain.Enums;
using Luiu.Domain.Models;
using Luiu.Service.DTOs.V1.Client;

namespace Luiu.Service.Mappings.v1
{
    internal class AuthProfile : Profile
    {
        public AuthProfile()
        {
            string MapRole(int roleId) => ((AppEnums.RoleType)roleId).ToString();

            CreateMap<TMember, LoginResultDTO>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => ((AppEnums.RoleType)src.RoleId).ToString()));
            CreateMap<LoginResultDTO, MemberDTO>();
            CreateMap<TMember, MemberDTO>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => ((AppEnums.RoleType)src.RoleId).ToString()));
        }
    }
}

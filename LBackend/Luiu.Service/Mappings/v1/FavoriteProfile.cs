using AutoMapper;
using Luiu.Domain.Models;
using Luiu.Service.DTOs.V1.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.Mappings.v1
{
    internal class FavoriteProfile : Profile
    {
        public FavoriteProfile()
        {
            CreateMap<TCollect, FavoriteItemDTO>()
                .ForMember(dest => dest.CollectId, opt => opt.MapFrom(src => src.CollectId))
                .ForMember(dest => dest.TargetId, opt => opt.MapFrom(src => src.ObjectId))
                .ForMember(dest => dest.Type, opt => opt.Ignore());
        }
    }
}

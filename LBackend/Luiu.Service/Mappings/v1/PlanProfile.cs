using AutoMapper;
using Luiu.Domain.Models;
using Luiu.Service.DTOs.V1.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.Mappings.v1
{
    internal class PlanProfile : Profile
    {
        public PlanProfile()
        {
            CreateMap<TTrip, HomeRecommendPlanDTO>()
                .ForMember(dest => dest.TripId, opt => opt.MapFrom(src => src.TripId))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.TripName))
                .ForMember(dest => dest.Tag, opt => opt.MapFrom(src => src.TripTag))
                .ForMember(dest => dest.CoverImage, opt => opt.MapFrom(src => src.PhotoUrl));
        }
    }
}

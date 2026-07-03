using AutoMapper;
using Luiu.Domain.Models;
using Luiu.Service.DTOs.V1.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.Mappings.v1
{
    internal class MemoryProfile : Profile
    {
        public MemoryProfile()
        {
            CreateMap<TMemory, MemberProfileMemoriesDTO>();
            CreateMap<TMemory, HomeHotMemoryDTO>();
        }
    }
}
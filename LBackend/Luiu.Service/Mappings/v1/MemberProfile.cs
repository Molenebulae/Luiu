using AutoMapper;
using Luiu.Domain.Models;
using Luiu.Service.DTOs.V1.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.Mappings.v1
{
    internal class MemberProfile : Profile
    {
        public MemberProfile()
        {
            CreateMap<TMember, MemberSettingUpdateDTO>();
        }
    }
}

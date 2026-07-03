using AutoMapper;
using Luiu.Domain.Enums;
using Luiu.Domain.Exceptions;
using Luiu.Domain.Models;
using Luiu.Service.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.Implementations
{
    public class AdminService : BaseService<AdminService>, IAdminService
    {
        public AdminService(LuiuDbContext context, ILogger<AdminService> logger, IMapper mapper) : base(context, logger, mapper)
        {
        }

        public async Task SetRecommendationAsync(string type, int id, bool isRecommended)
        {
            _logger.LogInformation("管理員請求: 類型={Type}, ID={Id}, 狀態={Status}", type, id, isRecommended);
            if (id <= 0)
            {
                _logger.LogWarning("無效的 ID: {Id}", id);
                throw new AppBadRequestException("無效的目標 ID");
            }

            var status = isRecommended ? AppEnums.OfficeOperStatus.Recommended : AppEnums.OfficeOperStatus.None;

            switch (type.ToLower())
            {
                case "trip":
                    var trip = await _context.TTrips.FindAsync(id);
                    if (trip == null) throw new AppBadRequestException($"找不到 ID 為 {id} 的行程");
                    trip.OfficeOper = (short)status;
                    break;
                case "memory":
                    var memory = await _context.TMemories.FindAsync(id);
                    if (memory == null) throw new AppBadRequestException($"找不到 ID 為 {id} 的回憶");
                    memory.OfficeOper = (short)status;
                    break;
                default:
                    throw new NotSupportedException("不支援的推薦類型");
            }
            await _context.SaveChangesAsync();
            _logger.LogInformation("成功更新推薦狀態: {Type} {Id}", type, id);
        }
    }
}

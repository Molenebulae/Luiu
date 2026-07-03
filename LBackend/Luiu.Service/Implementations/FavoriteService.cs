using AutoMapper;
using AutoMapper.Execution;
using Azure.Core;
using Azure.Storage.Blobs.Models;
using Luiu.Domain.Enums;
using Luiu.Domain.Exceptions;
using Luiu.Domain.Models;
using Luiu.Service.DTOs.V1.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using static Luiu.Domain.Enums.AppEnums;

namespace Luiu.Service.Implementations
{
    public class FavoriteService : BaseService<FavoriteService>
    {
        private readonly MemberService _memberService;
        public FavoriteService(LuiuDbContext context, ILogger<FavoriteService> logger, IMapper mapper, MemberService memberService) : base(context, logger, mapper)
        {
            _memberService = memberService;
        }

        public async Task<List<FavoriteItemDTO>> GetUserFavoritesAsync(string userId)
        {
            var memberId = await _memberService.GetMemberIdByUserIdAsync(userId);

            // 1. 取得所有收藏紀錄
            var collects = await _context.TCollects
                .Where(c => c.MemberId == memberId)
                .ToListAsync();

            var results = new List<FavoriteItemDTO>();

            // 2. 分類處理
            foreach (var item in collects)
            {
                switch ((CollectType)item.TypeId)
                {
                    case CollectType.Plan:
                        var plan = await _context.TTrips.FindAsync(item.ObjectId);
                        if (plan != null) results.Add(new FavoriteItemDTO
                        {
                            CollectId = item.CollectId,  // 正確：對應 tCollects 的 PK
                            TargetId = plan.TripId,      // 正確：對應物件 ID
                            UserId = userId,
                            Type = "Plan",
                            Title = plan.TripName,
                            SubTitle = $"{plan.StartDate:yyyy-MM-dd} ~ {plan.EndDate:yyyy-MM-dd}",
                            ImageUrl = plan.PhotoUrl
                        });
                        break;
                    case CollectType.Memory:
                        var memory = await _context.TMemories.FindAsync(item.ObjectId);
                        if (memory != null) results.Add(new FavoriteItemDTO
                        {
                            CollectId = item.CollectId,
                            TargetId = memory.MemoryId,
                            UserId = userId,
                            Type = "Memory",
                            Title = memory.Title,
                            SubTitle = $"{memory.StartDate:yyyy-MM-dd} ~ {memory.EndDate:yyyy-MM-dd}",
                            ImageUrl = memory.CoverImage
                        });
                        break;
                    case CollectType.Spot:
                        var spot = await _context.TSpots.FindAsync(item.ObjectId);
                        if (spot != null) results.Add(new FavoriteItemDTO
                        {
                            CollectId = item.CollectId,
                            TargetId = spot.SpotId,
                            UserId = userId,
                            Type = "Spot",
                            Title = spot.SpotName,
                            SubTitle = spot.Address,
                            ImageUrl = spot.PhotoUrl,
                            Rating = spot.Rating,
                            UserRatingCount = spot.UserRatingCount,
                            Longitude = spot.Longitude,
                            Latitude = spot.Latitude
                        });
                        break;
                }
            }
            return results;
        }

        public async Task AddFavoriteAsync(string userId, FavoriteAddDTO dto)
        {
            // TODO: 檢查目標有沒有存在過

            var memberId = await _memberService.GetMemberIdByUserIdAsync(userId);

            // 檢查類型是否合法，不合法直接結束
            if (!Enum.TryParse<CollectType>(dto.Type, out var typeEnum))
            {
                _logger.LogError("無效的收藏類型: {Type}", dto.Type);
                throw new AppBadRequestException("無效的收藏類型");
            }

            int typeId = (int)typeEnum;

            // 檢查是否已經收藏過，已存在直接結束
            var exists = await _context.TCollects
                .AnyAsync(c => c.MemberId == memberId
                            && c.ObjectId == dto.TargetId
                            && c.TypeId == typeId);

            if (exists)
            {
                _logger.LogWarning("使用者 {MemberId} 嘗試重複收藏項目 {TargetId}", memberId, dto.TargetId);
                throw new AppConflictException("項目已被收藏");
            }

            //執行新增 (剩下的就是正常的流程)
            var collect = new TCollect
            {
                MemberId = memberId,
                TypeId = typeId,
                ObjectId = dto.TargetId,
                CollectTime = DateTime.UtcNow
            };

            _context.TCollects.Add(collect);
            await _context.SaveChangesAsync();

            _logger.LogInformation("使用者 {MemberId} 已成功收藏項目 {TargetId}", memberId, dto.TargetId);
        }

        public async Task RemoveFavoriteAsync(string userId, int targetId, string type)
        {
            var memberId = await _memberService.GetMemberIdByUserIdAsync(userId);
            int typeId = (int)Enum.Parse<CollectType>(type);

            _logger.LogInformation("嘗試移除：MemberId={Mid}, TargetId={Tid}, TypeId={Tid_db}", memberId, targetId, typeId);

            var favorite = await _context.TCollects.FirstOrDefaultAsync(f =>
                f.MemberId == memberId && f.ObjectId == targetId && f.TypeId == typeId);

            if (favorite != null)
            {
                _context.TCollects.Remove(favorite);
                await _context.SaveChangesAsync();
                _logger.LogInformation("成功移除資料庫紀錄");
            }
            else
            {
                _logger.LogWarning("找不到符合條件的收藏紀錄，無法移除");
                throw new AppNotFoundException("找不到符合條件的收藏紀錄，無法移除");
            }
        }
    }
}

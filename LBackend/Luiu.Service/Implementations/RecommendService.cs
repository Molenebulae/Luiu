using AutoMapper;
using Luiu.Domain.Models;
using Luiu.Service.DTOs.V1.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Luiu.Service.Implementations
{
    public class RecommendService : BaseService<RecommendService>
    {
        public RecommendService(LuiuDbContext context, ILogger<RecommendService> logger, IMapper mapper, IMemoryCache cache) 
            : base(context, logger, mapper) 
        { 
        }

        public async Task<List<RecommendedUserDTO>> GetRecommendedUsersAsync(string? currentUserId, int limit = 5)
        {
            int currentMemberId = 0;
            List<int> followedMemberIds = new();

            if (!string.IsNullOrEmpty(currentUserId))
            {
                var currentMember = await _context.TMembers
                    .Select(m => new { m.MemberId, m.UserId })
                    .FirstOrDefaultAsync(m => m.UserId == currentUserId);
                
                if (currentMember != null)
                {
                    currentMemberId = currentMember.MemberId;
                    followedMemberIds = await _context.TFollows
                        .Where(f => f.FollowerId == currentMemberId)
                        .Select(f => f.FollowingId)
                        .ToListAsync();
                }
            }

            var query = _context.TMembers.AsQueryable();

            if (currentMemberId > 0)
            {
                query = query.Where(m => m.MemberId != currentMemberId && !followedMemberIds.Contains(m.MemberId));
            }

            var recommendedUsers = await query
                .OrderBy(m => Guid.NewGuid()) // 會在資料庫轉譯成 ORDER BY NEWID()，隨機排序
                .Take(limit)
                .Select(m => new RecommendedUserDTO
                {
                    UserId = m.UserId,
                    Name = m.Name,
                    AvatarUrl = m.AvatarUrl ?? string.Empty,
                    Bio = m.Bio ?? string.Empty,
                    IsFollowing = false
                })
                .ToListAsync();

            return recommendedUsers;
        }
    }
}

using AutoMapper;
using Luiu.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Luiu.Service.Implementations
{
    public class FollowService : BaseService<FollowService>
    {
        public FollowService(LuiuDbContext context, ILogger<FollowService> logger, IMapper mapper) : base(context, logger, mapper) { }

        private async Task<int> GetMemberIdByUserIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return 0;

            var member = await _context.TMembers
                .Select(m => new { m.MemberId, m.UserId })
                .FirstOrDefaultAsync(m => m.UserId == userId);

            return member?.MemberId ?? 0;
        }

        public async Task<bool> CreateFollowAsync(string currentUserId, string followingId)
        {
            int followerMemberId = await GetMemberIdByUserIdAsync(currentUserId);
            int followingMemberId = await GetMemberIdByUserIdAsync(followingId);
            _logger.LogInformation("follower: {er}, following: {ing}", followerMemberId, followingMemberId);


            // 防禦：確保雙方會員在資料庫中都存在
            if (followerMemberId == 0 || followingMemberId == 0)
            {
                return false;
            }

            var newFollow = new TFollow
            {
                FollowerId = followerMemberId, // 寫入整數 int
                FollowingId = followingMemberId, // 寫入整數 int
                CreateTime = DateTime.UtcNow
            };

            _context.TFollows.Add(newFollow);
            var rowsAffected = await _context.SaveChangesAsync();

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteFollowAsync(string currentUserId, string followingId)
        {
            int followerMemberId = await GetMemberIdByUserIdAsync(currentUserId);
            int followingMemberId = await GetMemberIdByUserIdAsync(followingId);

            if (followerMemberId == 0 || followingMemberId == 0)
            {
                return false;
            }

            // 依據整數複合主鍵找出紀錄
            var followRecord = await _context.TFollows
                .FirstOrDefaultAsync(f => f.FollowerId == followerMemberId && f.FollowingId == followingMemberId);

            if (followRecord == null)
            {
                return false;
            }

            _context.TFollows.Remove(followRecord);
            var rowsAffected = await _context.SaveChangesAsync();

            return rowsAffected > 0;
        }

        public async Task<bool> IsFollowingAsync(string currentUserId, string followingId)
        {
            int followerMemberId = await GetMemberIdByUserIdAsync(currentUserId);
            int followingMemberId = await GetMemberIdByUserIdAsync(followingId);
            _logger.LogInformation("follower: {er}, following: {ing}", followerMemberId, followingMemberId);

            if (followerMemberId == 0 || followingMemberId == 0)
            {
                return false;
            }

            return await _context.TFollows
                        .AnyAsync(f => f.FollowerId == followerMemberId && f.FollowingId == followingMemberId);
        }
    }
}

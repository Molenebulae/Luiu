using AutoMapper;
using Luiu.Domain.Exceptions;
using Luiu.Domain.Models;
using Luiu.Service.DTOs.V1.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Luiu.Service.Implementations
{
    public class MemberService : BaseService<MemberService>
    {
        public MemberService(LuiuDbContext context, ILogger<MemberService> logger, IMapper mapper) : base(context, logger, mapper)
        {
        }

        public async Task<int> GetMemberIdByUserIdAsync(string userId)
        {
            var member = await _context.TMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (member == null)
            {
                _logger.LogWarning("找不到使用者 ID: {UserId}", userId);
                return 0;
            }

            return member.MemberId;
        }

        public async Task<MemberProfileDTO> GetProfileAsync(string userId)
        {
            var member = await _context.TMembers.FirstOrDefaultAsync(m => m.UserId == userId);
            if (member == null) throw new AppNotFoundException("找不到該用戶");

            // 取得ID
            int mId = member.MemberId;
            _logger.LogInformation("取得MemberID: {id}", mId);

            // 取得統計數量
            int tripCount = await _context.TTrips.CountAsync(t => t.OwnerId == mId && t.IsDeleted == false);
            int memoryCount = await _context.TMemories.CountAsync(m => m.UserId == mId);
            int collectCount = await _context.TCollects.CountAsync(m => m.MemberId == mId);
            int followerCount = await _context.TFollows.CountAsync(f => f.FollowingId == mId); // 誰關注我
            int followingCount = await _context.TFollows.CountAsync(f => f.FollowerId == mId); // 我關注誰
            _logger.LogInformation("取得數量{trip}, {memory}, {collect}, {follower}, {followingCount}", tripCount, memoryCount, collectCount, followerCount, followingCount);

            return new MemberProfileDTO
            {
                UserId = userId,
                Name = member.Name,
                AvatarUrl = member.AvatarUrl,
                Bio = member.Bio,
                TripCount = tripCount,
                MemoryCount = memoryCount,
                CollectCount = collectCount,
                FollowerCount = followerCount,
                FollowingCount = followingCount,
            };
        }

        public async Task<string> UpdateMemberAsync(string userId, MemberProfileUpdateDTO request)
        {
            var member = await _context.TMembers.FirstOrDefaultAsync(m => m.UserId == userId);
            if (member == null) throw new AppNotFoundException("找不到該會員");

            // 判斷UserId 有沒有被使用過
            if (userId != request.UserId)
            {
                bool isExist = await _context.TMembers.AnyAsync(m => m.UserId == request.UserId);
                if (isExist) throw new AppBadRequestException("這個使用者ID 已被使用");

                member.UserId = request.UserId;
            }
            _logger.LogInformation("使用者ID 沒有被使用過");

            member.Name = request.Name;
            member.Bio = request.Bio;
            if (!string.IsNullOrEmpty(request.AvatarUrl)) member.AvatarUrl = request.AvatarUrl;
            _logger.LogInformation("member.AvatarUrl: {url}", member.AvatarUrl);

            await _context.SaveChangesAsync();

            return member.UserId;
        }

        public async Task<MemberSettingUpdateDTO> UpdateSettingsAsync(string currentUserId, MemberSettingUpdateDTO dto)
        {
            // 驗證目前登入的會員帳號是否存在
            var member = await _context.TMembers.FirstOrDefaultAsync(m => m.UserId == currentUserId);

            if (member == null)
            {
                throw new AppNotFoundException("找不到對應的會員帳號資料");
            }

            // 電話號碼
            if (!string.IsNullOrEmpty(dto.Phone) && dto.Phone.Length != 10)
            {
                throw new AppBadRequestException("電話號碼格式不正確，必須為 10 碼數字");
                throw new Exception("電話號碼格式不正確，必須為 10 碼數字");
            }

            // 3. 欄位資料對齊與更新
            member.Name = dto.Name;
            member.Phone = dto.Phone;
            member.Birthday = dto.Birthday;
            member.Gender = (byte)dto.Gender;
            member.AvatarUrl = dto.AvatarUrl;
            member.UpdateDate = DateTime.UtcNow; // 強制改寫最後更新時間

            _context.TMembers.Update(member);

            // 寫入資料庫
            await _context.SaveChangesAsync();

            var response = _mapper.Map<MemberSettingUpdateDTO>(member);
            _logger.LogInformation("更新結果: {response}", response);

            return response;


        }

        public async Task ChangePasswordAsync(string currentUserId, ChangePasswordDTO dto)
        {
            // 撈出該會員的實體資料
            var member = await _context.TMembers.FirstOrDefaultAsync(m => m.UserId == currentUserId);
            if (member == null)
            {
                throw new AppNotFoundException("找不到該會員帳號");
            }

            // 舊密碼檢查
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, member.Password))
            {
                throw new AppBadRequestException("目前輸入的舊密碼不正確");
            }

            // 避免一樣的密碼
            if (dto.CurrentPassword == dto.NewPassword)
            {
                throw new AppBadRequestException("新密碼不可與目前使用的舊密碼相同");
            }

            // 寫入
            member.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            member.UpdateDate = DateTime.UtcNow;
            _context.TMembers.Update(member);

            _logger.LogInformation("密碼更新成功");
            await _context.SaveChangesAsync();
        }

        public async Task SoftDeleteMemberAsync(string currentUserId)
        {
            // 使用 UserId 作為查詢條件，且排除已軟刪除者
            var member = await _context.TMembers.FirstOrDefaultAsync(m => m.UserId == currentUserId && !m.IsDelete);

            if (member == null)
            {
                throw new AppNotFoundException("找不到該會員帳號或帳號已失效");
            }

            member.IsDelete = true;
            member.DeleteTime = DateTime.UtcNow;
            member.UpdateDate = DateTime.UtcNow;
            _context.TMembers.Update(member);

            _logger.LogInformation("帳號刪除成功");
            await _context.SaveChangesAsync();
        }
    }
}

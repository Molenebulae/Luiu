using Luiu.Domain.DTOs;
using Luiu.Service.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers.V1.Client
{
    [Authorize]
    public class FollowsController : BaseController<FollowsController>
    {
        private readonly FollowService _followService;
        public FollowsController(ILogger<FollowsController> logger, FollowService followService) : base(logger)
        {
            _followService = followService;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        // POST: api/v1/Follow/{followingId}
        [HttpPost("{followingId}")]
        public async Task<ActionResult<ResultDTO<object>>> CreateFollow(string followingId)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogInformation("使用者未登入");
                return UnauthorizedFail("使用者未登入");
            }

            if (currentUserId == followingId)
            {
                _logger.LogInformation("你不能追蹤你自己");
                return BadRequestFail("你不能追蹤你自己");
            }

            var isAlreadyFollowing = await _followService.IsFollowingAsync(currentUserId, followingId);
            if (isAlreadyFollowing)
            {
                _logger.LogInformation("你已經追蹤過此會員");
                return ConflictFail("你已經追蹤過此會員");
            }

            var result = await _followService.CreateFollowAsync(currentUserId, followingId);
            if (!result)
            {
                _logger.LogInformation("建立追蹤失敗，目標會員可能不存在");
                return BadRequestFail("建立追蹤失敗，目標會員可能不存在");
            }

            return CreatedSuccess<object>("已成功建立追蹤關係");
        }

        // DELETE: api/v1/follows/{followingId}
        [HttpDelete("{followingId}")]
        public async Task<ActionResult<ResultDTO<object>>> DeleteFollow(string followingId)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogInformation("使用者未登入");
                return UnauthorizedFail("使用者未登入");
            }

            // 檢查追蹤關係是否存在（防禦 404 Not Found）
            var isAlreadyFollowing = await _followService.IsFollowingAsync(currentUserId, followingId);
            if (!isAlreadyFollowing)
            {
                _logger.LogInformation("追蹤關係不存在，無法取消追蹤");
                return NotFoundFail("追蹤關係不存在，無法取消追蹤");
            }

            // 呼叫服務層執行刪除
            var result = await _followService.DeleteFollowAsync(currentUserId, followingId);
            if (!result)
            {
                _logger.LogInformation("取消追蹤失敗");
                return BadRequestFail("取消追蹤失敗");
            }

            return Success<object>("已成功移除追蹤關係");
        }

        // GET: api/v1/follows/{followingId}/status
        [HttpGet("{followingId}/status")]
        public async Task<ActionResult<ResultDTO<bool>>> CheckFollowStatus(string followingId)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Success(false, "使用者未登入");
            }

            var isFollowing = await _followService.IsFollowingAsync(currentUserId, followingId);
            return Success(isFollowing, "查詢追蹤狀態成功");
        }
    }
}

using Luiu.Domain.DTOs;
using Luiu.Service.DTOs.V1.Client;
using Luiu.Service.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Formats.Asn1;
using System.Security.Claims;

namespace API.Controllers.V1.Client
{
    // 注意：這裡移除了 [Route] 與 [ApiVersion]，因此會繼承 BaseController 的 [Route("api/v{version:apiVersion}/[controller]")]
    // 也就是這隻 API 的路由會變成：/api/v1/Memory
    // 若前端 axios 是設定打 /memories，請記得將前端的 url 改為 /Memory，或是把 [Route("api/v{version:apiVersion}/memories")] 加回來。
    public class MemoryController : BaseController<MemoryController>
    {
        private readonly MemoryService _memoryService;

        public MemoryController(ILogger<MemoryController> logger, MemoryService memoryService) : base(logger)
        {
            _memoryService = memoryService;
        }

        [HttpGet]
        public async Task<ActionResult<ResultDTO<List<MemoryListResponseDTO>>>> GetMemories([FromQuery] MemoryQueryParameters parameters)
        {
            _logger.LogInformation("取得回憶列表");
            _logger.LogInformation("查詢條件: {data}", parameters);

            var result = await _memoryService.GetMemoriesAsync(parameters);

            _logger.LogInformation("取得回憶列表成功");
            return Success(result, "取得列表成功");
        }           

        [HttpGet("{id}")]
        public async Task<ActionResult<ResultDTO<MemoryDetailResponseDTO>>> GetMemoryDetail(int id)
        {
            _logger.LogInformation("取得回憶詳細資料, ID: {id}", id);

            var result = await _memoryService.GetMemoryDetailAsync(id);

            _logger.LogInformation("成功取得詳細資料: {result}", result);
            return Success(result, "取得詳細資料成功");
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ResultDTO<MemoryDetailResponseDTO>>> CreateMemory([FromBody] MemoryCreateRequestDTO request)
        {
            _logger.LogInformation("新增回憶");
            _logger.LogInformation("取得資料: {data}", request);

            // 從 JWT Token 中抓取這個人的字串 UserId
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdStr))
            {
                return UnauthorizedFail("憑證無效，請重新登入");
            }

            var result = await _memoryService.CreateMemoryAsync(request, currentUserIdStr);

            _logger.LogInformation("新增回憶成功");
            return Success(result, "新增成功");
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<ResultDTO<MemoryDetailResponseDTO>>> UpdateMemory(int id, [FromBody] MemoryCreateRequestDTO request)
        {
            _logger.LogInformation("更新回憶，ID: {id}", id);

            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdStr))
            {
                return UnauthorizedFail("憑證無效，請重新登入");
            }

            var result = await _memoryService.UpdateMemoryAsync(id, request, currentUserIdStr);

            _logger.LogInformation("更新回憶成功");
            return Success(result, "更新成功");
        }

        [HttpPost("{id}/like")]
        [Authorize]
        public async Task<ActionResult<ResultDTO<bool>>> ToggleLike(int id, [FromQuery] bool isLike)
        {
            _logger.LogInformation($"修改按讚狀態，MemoryId: {id}, isLike: {isLike}");

            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdStr))
            {
                return UnauthorizedFail("憑證無效，請重新登入");
            }

            var success = await _memoryService.UpdateLikeCountAsync(id, isLike);
            if (!success)
            {
                return NotFoundFail(false, "找不到該旅遊回憶");
            }

            return Success(true, "更新按讚成功");
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<ResultDTO<bool>>> DeleteMemory(int id)
        {
            _logger.LogInformation("刪除貼文，ID: {id}", id);

            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdStr))
            {
                return UnauthorizedFail("憑證無效，請重新登入");
            }

            var success = await _memoryService.DeleteMemoryAsync(id, currentUserIdStr);
            if (!success)
            {
                return NotFoundFail(false, "刪除失敗，找不到貼文");
            }

            return Success(true, "刪除貼文成功");
        }
        // Get api/v1/Memory/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ResultDTO<List<MemberProfileMemoriesDTO>>>> GetMemoriesByUserId(string userId)
        {
            _logger.LogInformation("執行取得 {userId} 的回憶列表", userId);
            var result = await _memoryService.GetMemoriesByUserAsync(userId);
            return Success(result, "取得回憶成功");
        }

        // Get api/v1/Memory/hot
        [HttpGet("hot")]
        public async Task<ActionResult<ResultDTO<List<HomeHotMemoryDTO>>>> GetHotMemories()
        {
            var data = await _memoryService.GetHotMemoriesAsync();

            return Success(data, "成功取得推薦回憶");
        }
    }
}

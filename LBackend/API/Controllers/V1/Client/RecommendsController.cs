using Luiu.Domain.DTOs;
using Luiu.Service.DTOs.V1.Client;
using Luiu.Service.Implementations;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers.V1.Client
{
    public class RecommendsController : BaseController<RecommendsController>
    {
        private readonly RecommendService _recommendService;

        public RecommendsController(ILogger<RecommendsController> logger, RecommendService recommendService) : base(logger)
        {
            _recommendService = recommendService;
        }

        // GET: api/v1/Recommends
        [HttpGet]
        public async Task<ActionResult<ResultDTO<List<RecommendedUserDTO>>>> GetRecommendedUsers()
        {
            _logger.LogInformation("取得推薦會員列表");
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var recommended = await _recommendService.GetRecommendedUsersAsync(currentUserId);
            return Success(recommended);
        }
    }
}

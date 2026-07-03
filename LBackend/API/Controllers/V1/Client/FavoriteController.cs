using Luiu.Domain.DTOs;
using Luiu.Domain.Models;
using Luiu.Service.DTOs.V1.Client;
using Luiu.Service.Implementations;
using Luiu.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Luiu.Domain.Enums.AppEnums;

namespace API.Controllers.V1.Client
{
    [Authorize]
    public class FavoriteController : BaseController<FavoriteController>
    {
        private readonly FavoriteService _favoriteService;
        public FavoriteController(ILogger<FavoriteController> logger, FavoriteService favoriteService) : base(logger)
        {
            _favoriteService = favoriteService;
        }

        // GET: api/v1/Favorite
        [HttpGet]
        public async Task<ActionResult<ResultDTO<List<FavoriteItemDTO>>>> GetMyFavorites()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("使用者 {UserId} 查詢收藏列表", userId);

            var favorites = await _favoriteService.GetUserFavoritesAsync(userId);
            return Success(favorites, "成功取得收藏列表");
        }

        // POST: api/v1/Favorite
        [HttpPost]
        public async Task<ActionResult<ResultDTO<object>>> AddFavorite([FromBody] FavoriteAddDTO dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("使用者 {UserId} 新增收藏項目 {TargetId} (類型: {Type})",
                userId, dto.TargetId, dto.Type);

            await _favoriteService.AddFavoriteAsync(userId, dto);
            return Success<object>("新增成功");
        }

        // DELETE: api/v1/Favorite/{targetId}/{type}
        [HttpDelete("{targetId}/{type}")]
        public async Task<ActionResult<ResultDTO<object>>> RemoveFavorite(int targetId, string type)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("使用者 {UserId} 移除收藏項目 {TargetId} (類型: {Type})",
                userId, targetId, type);

            await _favoriteService.RemoveFavoriteAsync(userId, targetId, type);
            return Success<object>("刪除成功");
        }
    }
}
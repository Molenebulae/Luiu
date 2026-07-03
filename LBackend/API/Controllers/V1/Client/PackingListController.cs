using Asp.Versioning;
using Luiu.Domain.DTOs;
using Luiu.Service.DTOs.V1.Client;
using Luiu.Service.Implementations;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.V1.Client
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/{userId}/plan-list/{tripId}/packing-list")]
    public class PackingListController : BaseController<PackingListController>
    {
        private readonly PackingListService _packingListService;

        public PackingListController(
            ILogger<PackingListController> logger,
            PackingListService packingListService) : base(logger)
        {
            _packingListService = packingListService;
        }

        [HttpGet("~/api/v{version:apiVersion}/{userId}/packing-list")]
        public async Task<ActionResult<ResultDTO<List<PackingListSummaryResponseDTO>>>> GetPackingListSummaries(
            [FromRoute] string userId)
        {
            _logger.LogInformation("Packing list summaries requested. UserId={UserId}", userId);
            var result = await _packingListService.GetPackingListSummariesAsync(userId);
            return Success(result, "取得行李清單列表成功");
        }

        [HttpGet]
        public async Task<ActionResult<ResultDTO<PackingListResponseDTO>>> GetPackingList(
            [FromRoute] string userId,
            [FromRoute] int tripId)
        {
            _logger.LogInformation("Packing list requested. UserId={UserId}, TripId={TripId}", userId, tripId);
            var result = await _packingListService.GetPackingListAsync(userId, tripId);
            if (result == null)
            {
                return Success<PackingListResponseDTO>(null, "尚未建立行李清單");
            }

            return Success(result, "取得行李清單成功");
        }

        [HttpPost]
        public async Task<ActionResult<ResultDTO<PackingListResponseDTO>>> CreatePackingList(
            [FromRoute] string userId,
            [FromRoute] int tripId,
            [FromBody] PackingListCreateRequestDTO request)
        {
            if (request == null)
            {
                return BadRequestFail("請提供行李清單資料");
            }

            _logger.LogInformation("Packing list create requested. UserId={UserId}, TripId={TripId}", userId, tripId);
            var result = await _packingListService.CreatePackingListAsync(userId, tripId, request);
            return Success(result, "新增行李清單成功");
        }

        [HttpPut]
        public async Task<ActionResult<ResultDTO<PackingListResponseDTO>>> UpdatePackingList(
            [FromRoute] string userId,
            [FromRoute] int tripId,
            [FromBody] PackingListUpdateRequestDTO request)
        {
            if (request == null)
            {
                return BadRequestFail("請提供行李清單資料");
            }

            _logger.LogInformation("Packing list update requested. UserId={UserId}, TripId={TripId}", userId, tripId);
            var result = await _packingListService.UpdatePackingListAsync(userId, tripId, request);
            return Success(result, "更新行李清單成功");
        }

        [HttpDelete]
        public async Task<ActionResult<ResultDTO<bool>>> DeletePackingList(
            [FromRoute] string userId,
            [FromRoute] int tripId)
        {
            _logger.LogInformation("Packing list delete requested. UserId={UserId}, TripId={TripId}", userId, tripId);
            var result = await _packingListService.DeletePackingListAsync(userId, tripId);
            return Success(result, "刪除行李清單成功");
        }

        [HttpPost("categories")]
        public async Task<ActionResult<ResultDTO<PackingCategoryResponseDTO>>> CreateCategory(
            [FromRoute] string userId,
            [FromRoute] int tripId,
            [FromBody] PackingCategoryCreateRequestDTO request)
        {
            if (request == null)
            {
                return BadRequestFail("請提供行李分類資料");
            }

            _logger.LogInformation("Packing category create requested. UserId={UserId}, TripId={TripId}", userId, tripId);
            var result = await _packingListService.CreateCategoryAsync(userId, tripId, request);
            return Success(result, "新增行李分類成功");
        }

        [HttpPut("categories/{categoryId}")]
        public async Task<ActionResult<ResultDTO<PackingCategoryResponseDTO>>> UpdateCategory(
            [FromRoute] string userId,
            [FromRoute] int tripId,
            [FromRoute] int categoryId,
            [FromBody] PackingCategoryUpdateRequestDTO request)
        {
            if (request == null)
            {
                return BadRequestFail("請提供行李分類資料");
            }

            _logger.LogInformation("Packing category update requested. UserId={UserId}, TripId={TripId}, CategoryId={CategoryId}", userId, tripId, categoryId);
            var result = await _packingListService.UpdateCategoryAsync(userId, tripId, categoryId, request);
            return Success(result, "更新行李分類成功");
        }

        [HttpDelete("categories/{categoryId}")]
        public async Task<ActionResult<ResultDTO<bool>>> DeleteCategory(
            [FromRoute] string userId,
            [FromRoute] int tripId,
            [FromRoute] int categoryId)
        {
            _logger.LogInformation("Packing category delete requested. UserId={UserId}, TripId={TripId}, CategoryId={CategoryId}", userId, tripId, categoryId);
            var result = await _packingListService.DeleteCategoryAsync(userId, tripId, categoryId);
            return Success(result, "刪除行李分類成功");
        }

        [HttpPost("categories/{categoryId}/items")]
        public async Task<ActionResult<ResultDTO<PackingItemResponseDTO>>> CreateItem(
            [FromRoute] string userId,
            [FromRoute] int tripId,
            [FromRoute] int categoryId,
            [FromBody] PackingItemCreateRequestDTO request)
        {
            if (request == null)
            {
                return BadRequestFail("請提供行李項目資料");
            }

            _logger.LogInformation("Packing item create requested. UserId={UserId}, TripId={TripId}, CategoryId={CategoryId}", userId, tripId, categoryId);
            var result = await _packingListService.CreateItemAsync(userId, tripId, categoryId, request);
            return Success(result, "新增行李項目成功");
        }

        [HttpPut("items/{itemId}")]
        public async Task<ActionResult<ResultDTO<PackingItemResponseDTO>>> UpdateItem(
            [FromRoute] string userId,
            [FromRoute] int tripId,
            [FromRoute] int itemId,
            [FromBody] PackingItemUpdateRequestDTO request)
        {
            if (request == null)
            {
                return BadRequestFail("請提供行李項目資料");
            }

            _logger.LogInformation("Packing item update requested. UserId={UserId}, TripId={TripId}, ItemId={ItemId}", userId, tripId, itemId);
            var result = await _packingListService.UpdateItemAsync(userId, tripId, itemId, request);
            return Success(result, "更新行李項目成功");
        }

        [HttpDelete("items/{itemId}")]
        public async Task<ActionResult<ResultDTO<bool>>> DeleteItem(
            [FromRoute] string userId,
            [FromRoute] int tripId,
            [FromRoute] int itemId)
        {
            _logger.LogInformation("Packing item delete requested. UserId={UserId}, TripId={TripId}, ItemId={ItemId}", userId, tripId, itemId);
            var result = await _packingListService.DeleteItemAsync(userId, tripId, itemId);
            return Success(result, "刪除行李項目成功");
        }
    }
}

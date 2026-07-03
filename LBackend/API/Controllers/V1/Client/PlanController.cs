using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Luiu.Service.Implementations;
using Luiu.Domain.DTOs;
using Luiu.Service.DTOs.V1.Client;

namespace API.Controllers.V1.Client
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/{userId}/plan-list")]
    [Route("{userId}/plan-list")]
    public class PlanController : BaseController<PlanController>
    {
        private readonly PlanService _planService;
        public PlanController(ILogger<PlanController> logger, PlanService planService) : base(logger)
        {
            _planService = planService;
        }

        // GET: api/v1/{userId}/plan-list
        [HttpGet]
        public async Task<ActionResult<ResultDTO<List<PlanListResponseDTO>>>>
            GetList(string userId)
        {
            // PlanList CRUD：取得指定會員的行程卡片列表。
            _logger.LogInformation("Plan list requested. UserId={UserId}", userId);
            var result = await _planService.GetPlanListAsync(userId);
            _logger.LogInformation("Plan list completed. UserId={UserId}, ResultCount={ResultCount}", userId, result.Count);
            return Success(result, "取得行程列表成功");
        }

        // GET: api/v1/{userId}/plan-list/{tripId}
        [HttpGet("{tripId}")]
        public async Task<ActionResult<ResultDTO<PlanDetailResponseDTO>>>
            GetDetail(string userId, int tripId)
        {
            _logger.LogInformation("Plan detail requested. UserId={UserId}, TripId={TripId}", userId, tripId);
            var result = await _planService.GetPlanDetailAsync(userId, tripId);
            _logger.LogInformation("Plan detail completed. UserId={UserId}, TripId={TripId}, DetailCount={DetailCount}", userId, tripId, result.TripDetails.Count);
            return Success(result, "取得行程細項成功");
        }

        // GET: api/v1/{userId}/plan-list/{tripId}/comments
        [HttpGet("{tripId}/comments")]
        public async Task<ActionResult<ResultDTO<List<TripCommentResponseDTO>>>>
            GetTripComments(string userId, int tripId)
        {
            _logger.LogInformation("Trip comments requested. UserId={UserId}, TripId={TripId}", userId, tripId);
            var result = await _planService.GetTripCommentsAsync(userId, tripId);
            _logger.LogInformation("Trip comments completed. UserId={UserId}, TripId={TripId}, CommentCount={CommentCount}", userId, tripId, result.Count);
            return Success(result, "取得行程留言成功");
        }


        // POST: api/v1/{userId}/plan-list
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ResultDTO<PlanListResponseDTO>>>
            Create(string userId, [FromForm] PlanListCreateRequestDTO request)
        {
            // PlanList CRUD：建立行程前先做最小必要欄位檢查。
            if (request == null || string.IsNullOrEmpty(request.TripName))
            {
                return BadRequestFail("請輸入行程名稱");
            }

            _logger.LogInformation("Plan create requested. UserId={UserId}", userId);
            var result = await _planService.CreatePlanAsync(userId, request);
            _logger.LogInformation("Plan create completed. UserId={UserId}, TripId={TripId}", userId, result.TripId);
            return Success(result, "新增行程成功");
        }

        // POST: api/v1/{userId}/plan-list/{tripId}
        [HttpPost("{tripId}")]
        public async Task<ActionResult<ResultDTO<PlanDetailItemDTO>>>
            CreateTripDetail(string userId, int tripId, [FromBody] TripDetailCreateRequestDTO request)
        {
            if (request == null)
            {
                return BadRequestFail("請提供行程明細資料");
            }

            _logger.LogInformation(
                "Trip detail create requested. UserId={UserId}, TripId={TripId}, SpotId={SpotId}, DayNumber={DayNumber}, SortOrder={SortOrder}",
                userId,
                tripId,
                request.SpotId,
                request.DayNumber,
                request.SortOrder);
            var result = await _planService.CreateTripDetailAsync(userId, tripId, request);
            _logger.LogInformation("Trip detail create completed. UserId={UserId}, TripId={TripId}, DetailId={DetailId}", userId, tripId, result.DetailId);
            return Success(result, "新增行程明細成功");
        }

        // POST: api/v1/{userId}/plan-list/{tripId}/comments
        [HttpPost("{tripId}/comments")]
        public async Task<ActionResult<ResultDTO<TripCommentResponseDTO>>>
            CreateTripComment(string userId, int tripId, [FromBody] TripCommentCreateRequestDTO request)
        {
            if (request == null)
            {
                return BadRequestFail("請提供留言資料");
            }

            _logger.LogInformation(
                "Trip comment create requested. UserId={UserId}, TripId={TripId}, ParentId={ParentId}",
                userId,
                tripId,
                request.ParentId);
            var result = await _planService.CreateTripCommentAsync(userId, tripId, request);
            _logger.LogInformation("Trip comment create completed. UserId={UserId}, TripId={TripId}, CommentId={CommentId}", userId, tripId, result.CommentId);
            return Success(result, "新增行程留言成功");
        }

        // PUT: api/v1/{userId}/plan-list/{tripId}
        [HttpPut("{tripId}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ResultDTO<PlanListResponseDTO>>>
            Update(string userId, int tripId, [FromForm] PlanListUpdateRequestDTO request)
        {
            // PlanList CRUD：更新行程卡片會用到的基本資料。
            if (request == null || string.IsNullOrEmpty(request.TripName))
            {
                return BadRequestFail("請輸入行程名稱");
            }

            _logger.LogInformation("Plan update requested. UserId={UserId}, TripId={TripId}", userId, tripId);
            var result = await _planService.UpdatePlanAsync(userId, tripId, request);
            _logger.LogInformation("Plan update completed. UserId={UserId}, TripId={TripId}", userId, result.TripId);
            return Success(result, "更新行程成功");
        }

        // PATCH: api/v1/{userId}/plan-list/{tripId}
        [HttpPatch("{tripId}")]
        public async Task<ActionResult<ResultDTO<UpdateTripSuggestResponseDTO>>>
            UpdateTripSuggest(string userId, int tripId, [FromBody] UpdateTripSuggestRequestDTO request)
        {
            if (request == null)
            {
                return BadRequestFail("請提供推薦狀態資料");
            }

            _logger.LogInformation("Trip suggest update requested. UserId={UserId}, TripId={TripId}, IsSuggest={IsSuggest}, OfficeOper={OfficeOper}", userId, tripId, request.IsSuggest, request.OfficeOper);
            var result = await _planService.UpdateTripSuggestAsync(userId, tripId, request);
            _logger.LogInformation("Trip suggest update completed. UserId={UserId}, TripId={TripId}, IsSuggest={IsSuggest}, OfficeOper={OfficeOper}", userId, tripId, result.IsSuggest, result.OfficeOper);
            return Success(result, "推薦狀態已更新");
        }

        // PUT: api/v1/{userId}/plan-list/{tripId}/{detailId}
        [HttpPut("{tripId}/{detailId}")]
        public async Task<ActionResult<ResultDTO<PlanDetailItemDTO>>>
            UpdateTripDetail(string userId, int tripId, int detailId, [FromBody] TripDetailUpdateRequestDTO request)
        {
            if (request == null)
            {
                return BadRequestFail("請提供行程明細資料");
            }

            _logger.LogInformation(
                "Trip detail update requested. UserId={UserId}, TripId={TripId}, DetailId={DetailId}, SpotId={SpotId}, DayNumber={DayNumber}, SortOrder={SortOrder}",
                userId,
                tripId,
                detailId,
                request.SpotId,
                request.DayNumber,
                request.SortOrder);
            var result = await _planService.UpdateTripDetailAsync(userId, tripId, detailId, request);
            _logger.LogInformation("Trip detail update completed. UserId={UserId}, TripId={TripId}, DetailId={DetailId}", userId, tripId, result.DetailId);
            return Success(result, "更新行程明細成功");
        }

        // PUT: api/v1/{userId}/plan-list/{tripId}/details/sync
        [HttpPut("{tripId}/details/sync")]
        public async Task<ActionResult<ResultDTO<List<PlanDetailItemDTO>>>>
            SyncTripDetails(string userId, int tripId, [FromBody] TripDetailSyncRequestDTO request)
        {
            if (request == null)
            {
                return BadRequestFail("請提供行程明細同步資料");
            }

            _logger.LogInformation(
                "Trip details sync requested. UserId={UserId}, TripId={TripId}, CreatedCount={CreatedCount}, UpdatedCount={UpdatedCount}, DeletedCount={DeletedCount}",
                userId,
                tripId,
                request.Created?.Count ?? 0,
                request.Updated?.Count ?? 0,
                request.DeletedDetailIds?.Count ?? 0);
            var result = await _planService.SyncTripDetailsAsync(userId, tripId, request);
            _logger.LogInformation("Trip details sync completed. UserId={UserId}, TripId={TripId}, DetailCount={DetailCount}", userId, tripId, result.Count);
            return Success(result, "同步行程明細成功");
        }

        // PUT: api/v1/{userId}/plan-list/{tripId}/comments/{commentId}
        [HttpPut("{tripId}/comments/{commentId}")]
        public async Task<ActionResult<ResultDTO<TripCommentResponseDTO>>>
            UpdateTripComment(string userId, int tripId, int commentId, [FromBody] TripCommentUpdateRequestDTO request)
        {
            if (request == null)
            {
                return BadRequestFail("請提供留言資料");
            }

            _logger.LogInformation("Trip comment update requested. UserId={UserId}, TripId={TripId}, CommentId={CommentId}", userId, tripId, commentId);
            var result = await _planService.UpdateTripCommentAsync(userId, tripId, commentId, request);
            _logger.LogInformation("Trip comment update completed. UserId={UserId}, TripId={TripId}, CommentId={CommentId}", userId, tripId, result.CommentId);
            return Success(result, "更新行程留言成功");
        }

        // DELETE: api/v1/{userId}/plan-list/{tripId}
        [HttpDelete("{tripId}")]
        public async Task<ActionResult<ResultDTO<bool>>>
            Delete(string userId, int tripId)
        {
            // PlanList CRUD：刪除採軟刪除，由 Service 設定 IsDeleted。
            _logger.LogInformation("Plan delete requested. UserId={UserId}, TripId={TripId}", userId, tripId);
            var result = await _planService.DeletePlanAsync(userId, tripId);
            _logger.LogInformation("Plan delete completed. UserId={UserId}, TripId={TripId}", userId, tripId);
            return Success(result, "刪除行程成功");
        }

        // DELETE: api/v1/{userId}/plan-list/{tripId}/{detailId}
        [HttpDelete("{tripId}/{detailId}")]
        public async Task<ActionResult<ResultDTO<bool>>>
            DeleteTripDetail(string userId, int tripId, int detailId)
        {
            _logger.LogInformation("Trip detail delete requested. UserId={UserId}, TripId={TripId}, DetailId={DetailId}", userId, tripId, detailId);
            var result = await _planService.DeleteTripDetailAsync(userId, tripId, detailId);
            _logger.LogInformation("Trip detail delete completed. UserId={UserId}, TripId={TripId}, DetailId={DetailId}", userId, tripId, detailId);
            return Success(result, "刪除行程明細成功");
        }

        // DELETE: api/v1/{userId}/plan-list/{tripId}/comments/{commentId}
        [HttpDelete("{tripId}/comments/{commentId}")]
        public async Task<ActionResult<ResultDTO<bool>>>
            DeleteTripComment(string userId, int tripId, int commentId)
        {
            _logger.LogInformation("Trip comment delete requested. UserId={UserId}, TripId={TripId}, CommentId={CommentId}", userId, tripId, commentId);
            var result = await _planService.DeleteTripCommentAsync(userId, tripId, commentId);
            _logger.LogInformation("Trip comment delete completed. UserId={UserId}, TripId={TripId}, CommentId={CommentId}", userId, tripId, commentId);
            return Success(result, "刪除行程留言成功");
        }
    }
}
